namespace Jellyfin.Plugin.SendToKindle.Jobs;

public sealed record SendJobSnapshot(
    Guid JobId,
    Guid ItemId,
    string Title,
    SendJobStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string Message,
    int PartsSent,
    int TotalParts);
