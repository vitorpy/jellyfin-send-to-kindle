using System.Collections.Concurrent;
using System.Threading.Channels;
using Jellyfin.Plugin.SendToKindle.Conversion;
using Jellyfin.Plugin.SendToKindle.Email;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SendToKindle.Jobs;

public sealed class SendJobQueue : BackgroundService, ISendJobQueue
{
    private const int MaximumRetainedJobs = 50;
    private readonly Channel<SendJob> _channel = Channel.CreateUnbounded<SendJob>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
    private readonly ConcurrentDictionary<Guid, SendJob> _jobs = new();
    private readonly ConcurrentDictionary<Guid, Guid> _activeItemJobs = new();
    private readonly object _enqueueLock = new();
    private readonly IBookConversionService _conversionService;
    private readonly ISmtpDeliveryService _smtpDeliveryService;
    private readonly ILogger<SendJobQueue> _logger;

    public SendJobQueue(
        IBookConversionService conversionService,
        ISmtpDeliveryService smtpDeliveryService,
        ILogger<SendJobQueue> logger)
    {
        _conversionService = conversionService;
        _smtpDeliveryService = smtpDeliveryService;
        _logger = logger;
    }

    public SendJobSnapshot Enqueue(BookSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        lock (_enqueueLock)
        {
            if (_activeItemJobs.TryGetValue(source.ItemId, out Guid existingId)
                && _jobs.TryGetValue(existingId, out SendJob? existingJob))
            {
                throw new DuplicateJobException(existingJob.Snapshot());
            }

            SendJob job = new(source);
            _jobs[job.JobId] = job;
            _activeItemJobs[source.ItemId] = job.JobId;
            if (!_channel.Writer.TryWrite(job))
            {
                _jobs.TryRemove(job.JobId, out _);
                _activeItemJobs.TryRemove(source.ItemId, out _);
                throw new InvalidOperationException("The Send to Kindle queue is not accepting new jobs.");
            }

            return job.Snapshot();
        }
    }

    public SendJobSnapshot? Get(Guid jobId)
    {
        return _jobs.TryGetValue(jobId, out SendJob? job) ? job.Snapshot() : null;
    }

    public IReadOnlyList<SendJobSnapshot> GetRecent(int limit)
    {
        int boundedLimit = Math.Clamp(limit, 1, MaximumRetainedJobs);
        return _jobs.Values
            .Select(job => job.Snapshot())
            .OrderByDescending(job => job.CreatedAt)
            .Take(boundedLimit)
            .ToArray();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (SendJob job in _channel.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
        {
            await ProcessJobAsync(job, stoppingToken).ConfigureAwait(false);
            _activeItemJobs.TryRemove(job.Source.ItemId, out _);
            TrimHistory();
        }
    }

    private async Task ProcessJobAsync(SendJob job, CancellationToken stoppingToken)
    {
        try
        {
            job.Update(SendJobStatus.Converting, "Converting book");
            await using ConversionResult result = await _conversionService
                .ConvertAsync(job.Source, stoppingToken)
                .ConfigureAwait(false);

            job.Update(SendJobStatus.Sending, "Sending converted book", 0, result.Files.Count);
            await _smtpDeliveryService.SendAsync(
                result.Files,
                job.Source.Title,
                (sent, total) => job.Update(
                    SendJobStatus.Sending,
                    $"Sent {sent} of {total} parts",
                    sent,
                    total),
                stoppingToken).ConfigureAwait(false);
            job.Update(
                SendJobStatus.Succeeded,
                result.Files.Count == 1 ? "Sent to Kindle" : $"Sent all {result.Files.Count} parts to Kindle",
                result.Files.Count,
                result.Files.Count);
        }
        catch (PartialDeliveryException exception)
        {
            _logger.LogError(exception, "Partially sent book {ItemId}", job.Source.ItemId);
            job.Update(
                SendJobStatus.Failed,
                exception.Message,
                exception.PartsSent,
                exception.TotalParts);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            job.Update(SendJobStatus.Failed, "Jellyfin stopped before the job completed");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to send book {ItemId} to Kindle", job.Source.ItemId);
            job.Update(SendJobStatus.Failed, exception.Message);
        }
    }

    private void TrimHistory()
    {
        SendJobSnapshot[] snapshots = _jobs.Values
            .Select(job => job.Snapshot())
            .OrderByDescending(job => job.CreatedAt)
            .ToArray();
        foreach (SendJobSnapshot oldJob in snapshots.Skip(MaximumRetainedJobs))
        {
            _jobs.TryRemove(oldJob.JobId, out _);
        }
    }
}
