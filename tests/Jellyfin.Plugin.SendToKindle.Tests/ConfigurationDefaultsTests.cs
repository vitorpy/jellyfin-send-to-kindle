using Jellyfin.Plugin.SendToKindle.Configuration;

namespace Jellyfin.Plugin.SendToKindle.Tests;

public sealed class ConfigurationDefaultsTests
{
    [Fact]
    public void PluginConfiguration_HasSensibleDefaults()
    {
        PluginConfiguration configuration = new();

        Assert.Equal(587, configuration.SmtpPort);
        Assert.Equal(SmtpSecurityMode.StartTls, configuration.SmtpSecurity);
        Assert.Equal("kcc-c2e", configuration.KccExecutable);
        Assert.Equal("ebook-convert", configuration.CalibreExecutable);
        Assert.Equal(18, configuration.AttachmentLimitMegabytes);
        Assert.Equal(30, configuration.ConversionTimeoutMinutes);
    }

    [Fact]
    public void KccOptions_HaveSensibleDefaults()
    {
        KccOptions options = new();

        Assert.Equal("KV", options.Profile);
        Assert.Equal("Auto", options.Gamma);
        Assert.Equal(0, options.Splitter);
        Assert.Equal(2, options.Cropping);
        Assert.Equal(1, options.CroppingPower);
        Assert.Equal(85, options.JpegQuality);
    }
}
