namespace Jellyfin.Plugin.SendToKindle.Conversion;

public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
