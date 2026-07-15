namespace Jellyfin.Plugin.SendToKindle.Controllers;

public sealed record PluginDiagnosticsResponse(
    bool WebActionRegistered,
    string WebActionMessage,
    bool SmtpPasswordFromEnvironment);
