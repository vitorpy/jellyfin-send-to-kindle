namespace Jellyfin.Plugin.SendToKindle.Conversion;

public sealed record ProcessRequest(
    string FileName,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    TimeSpan Timeout);
