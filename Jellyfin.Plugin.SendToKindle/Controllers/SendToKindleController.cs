using System.Reflection;
using Jellyfin.Plugin.SendToKindle.Configuration;
using Jellyfin.Plugin.SendToKindle.Conversion;
using Jellyfin.Plugin.SendToKindle.Email;
using Jellyfin.Plugin.SendToKindle.Jobs;
using Jellyfin.Plugin.SendToKindle.WebIntegration;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.SendToKindle.Controllers;

[ApiController]
[Route("SendToKindle")]
[Authorize(Policy = Policies.RequiresElevation)]
public sealed class SendToKindleController : ControllerBase
{
    private readonly ILibraryManager _libraryManager;
    private readonly ISendJobQueue _jobQueue;
    private readonly IProcessRunner _processRunner;
    private readonly ISmtpDeliveryService _smtpDeliveryService;
    private readonly IPluginConfigurationAccessor _configurationAccessor;
    private readonly IWebIntegrationStatus _webIntegrationStatus;

    public SendToKindleController(
        ILibraryManager libraryManager,
        ISendJobQueue jobQueue,
        IProcessRunner processRunner,
        ISmtpDeliveryService smtpDeliveryService,
        IPluginConfigurationAccessor configurationAccessor,
        IWebIntegrationStatus webIntegrationStatus)
    {
        _libraryManager = libraryManager;
        _jobQueue = jobQueue;
        _processRunner = processRunner;
        _smtpDeliveryService = smtpDeliveryService;
        _configurationAccessor = configurationAccessor;
        _webIntegrationStatus = webIntegrationStatus;
    }

    [HttpPost("Jobs")]
    [ProducesResponseType(typeof(SendJobSnapshot), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(SendJobSnapshot), StatusCodes.Status409Conflict)]
    public ActionResult<SendJobSnapshot> Enqueue([FromBody] EnqueueSendRequest request)
    {
        BaseItem item = _libraryManager.GetItemById(request.ItemId)
            ?? throw new KeyNotFoundException($"Jellyfin item '{request.ItemId}' was not found.");
        if (item is not Book)
        {
            return BadRequest("Send to Kindle is available only for Books library items.");
        }

        if (string.IsNullOrWhiteSpace(item.Path) || !BookConversionService.IsSupportedExtension(item.Path))
        {
            return BadRequest("The selected book does not have a supported EPUB, PDF, MOBI, AZW, AZW3, CBR, or CBZ file.");
        }

        BookSource source = new(item.Id, item.Path, item.Name, ReadAuthor(item));
        try
        {
            SendJobSnapshot job = _jobQueue.Enqueue(source);
            return AcceptedAtAction(nameof(GetJob), new { jobId = job.JobId }, job);
        }
        catch (DuplicateJobException exception)
        {
            return Conflict(exception.ExistingJob);
        }
    }

    [HttpGet("Jobs/{jobId:guid}")]
    [ProducesResponseType(typeof(SendJobSnapshot), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<SendJobSnapshot> GetJob(Guid jobId)
    {
        SendJobSnapshot? job = _jobQueue.Get(jobId);
        return job is null ? NotFound() : Ok(job);
    }

    [HttpGet("Jobs")]
    public ActionResult<IReadOnlyList<SendJobSnapshot>> GetRecentJobs([FromQuery] int limit = 20)
    {
        return Ok(_jobQueue.GetRecent(limit));
    }

    [HttpGet("Diagnostics")]
    public ActionResult<PluginDiagnosticsResponse> GetDiagnostics()
    {
        return Ok(new PluginDiagnosticsResponse(
            _webIntegrationStatus.IsRegistered,
            _webIntegrationStatus.Message,
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(SmtpDeliveryService.PasswordEnvironmentVariable))));
    }

    [HttpPost("Diagnostics/Smtp")]
    public async Task<ActionResult<DependencyCheckResult>> CheckSmtp(CancellationToken cancellationToken)
    {
        try
        {
            await _smtpDeliveryService.CheckConnectionAsync(cancellationToken).ConfigureAwait(false);
            return Ok(new DependencyCheckResult("SMTP", true, "Connection and authentication succeeded."));
        }
        catch (Exception exception)
        {
            return Ok(new DependencyCheckResult("SMTP", false, exception.GetBaseException().Message));
        }
    }

    [HttpPost("Diagnostics/Converters")]
    public async Task<ActionResult<IReadOnlyList<DependencyCheckResult>>> CheckConverters(
        CancellationToken cancellationToken)
    {
        PluginConfiguration configuration = _configurationAccessor.Current;
        DependencyCheckResult[] results =
        {
            await CheckExecutableAsync("KCC", configuration.KccExecutable, cancellationToken).ConfigureAwait(false),
            await CheckExecutableAsync("Calibre", configuration.CalibreExecutable, cancellationToken).ConfigureAwait(false),
        };
        return Ok(results);
    }

    private async Task<DependencyCheckResult> CheckExecutableAsync(
        string name,
        string executable,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(executable))
        {
            return new DependencyCheckResult(name, false, "Executable is not configured.");
        }

        try
        {
            ProcessResult result = await _processRunner.RunAsync(
                new ProcessRequest(
                    executable,
                    new[] { "--version" },
                    Path.GetTempPath(),
                    TimeSpan.FromSeconds(15)),
                cancellationToken).ConfigureAwait(false);
            string output = string.IsNullOrWhiteSpace(result.StandardOutput)
                ? result.StandardError.Trim()
                : result.StandardOutput.Trim();
            string message = string.IsNullOrWhiteSpace(output)
                ? $"Executable started and exited with code {result.ExitCode}."
                : output.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            return new DependencyCheckResult(name, result.ExitCode == 0, message);
        }
        catch (Exception exception)
        {
            return new DependencyCheckResult(name, false, exception.GetBaseException().Message);
        }
    }

    private static string ReadAuthor(BaseItem item)
    {
        foreach (string propertyName in new[] { "Author", "Authors" })
        {
            PropertyInfo? property = item.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            object? value = property?.GetValue(item);
            if (value is string author && !string.IsNullOrWhiteSpace(author))
            {
                return author;
            }

            if (value is IEnumerable<string> authors)
            {
                string joined = string.Join(", ", authors.Where(author => !string.IsNullOrWhiteSpace(author)));
                if (!string.IsNullOrWhiteSpace(joined))
                {
                    return joined;
                }
            }
        }

        return "Unknown author";
    }
}
