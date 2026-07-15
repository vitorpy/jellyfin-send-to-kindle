namespace Jellyfin.Plugin.SendToKindle.Jobs;

internal sealed class SendJob
{
    private readonly object _lock = new();
    private SendJobStatus _status = SendJobStatus.Queued;
    private DateTimeOffset _updatedAt;
    private string _message = "Waiting for conversion";
    private int _partsSent;
    private int _totalParts;

    public SendJob(BookSource source)
    {
        JobId = Guid.NewGuid();
        Source = source;
        CreatedAt = DateTimeOffset.UtcNow;
        _updatedAt = CreatedAt;
    }

    public Guid JobId { get; }

    public BookSource Source { get; }

    public DateTimeOffset CreatedAt { get; }

    public void Update(SendJobStatus status, string message, int partsSent = 0, int totalParts = 0)
    {
        lock (_lock)
        {
            _status = status;
            _message = message;
            _partsSent = partsSent;
            _totalParts = totalParts;
            _updatedAt = DateTimeOffset.UtcNow;
        }
    }

    public SendJobSnapshot Snapshot()
    {
        lock (_lock)
        {
            return new SendJobSnapshot(
                JobId,
                Source.ItemId,
                Source.Title,
                _status,
                CreatedAt,
                _updatedAt,
                _message,
                _partsSent,
                _totalParts);
        }
    }
}
