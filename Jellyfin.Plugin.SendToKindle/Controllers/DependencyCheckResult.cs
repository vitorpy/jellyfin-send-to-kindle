namespace Jellyfin.Plugin.SendToKindle.Controllers;

public sealed record DependencyCheckResult(string Name, bool Success, string Message);
