namespace Jellyfin.Plugin.SendToKindle.Jobs;

public sealed record BookSource(Guid ItemId, string Path, string Title, string Author);
