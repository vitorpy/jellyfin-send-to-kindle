using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.SendToKindle.Configuration;

public sealed class PluginConfiguration : BasePluginConfiguration
{
    public string SmtpHost { get; set; } = string.Empty;

    public int SmtpPort { get; set; } = 587;

    public SmtpSecurityMode SmtpSecurity { get; set; } = SmtpSecurityMode.StartTls;

    public string SmtpUsername { get; set; } = string.Empty;

    public string SmtpPassword { get; set; } = string.Empty;

    public string SenderAddress { get; set; } = string.Empty;

    public string KindleAddress { get; set; } = string.Empty;

    public string KccExecutable { get; set; } = "kcc-c2e";

    public string CalibreExecutable { get; set; } = "ebook-convert";

    public string TemporaryDirectory { get; set; } = string.Empty;

    public int AttachmentLimitMegabytes { get; set; } = 18;

    public int ConversionTimeoutMinutes { get; set; } = 30;

    public KccOptions Kcc { get; set; } = new();
}
