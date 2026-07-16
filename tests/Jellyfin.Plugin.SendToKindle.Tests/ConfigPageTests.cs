using System.Reflection;

namespace Jellyfin.Plugin.SendToKindle.Tests;

public sealed class ConfigPageTests
{
    [Fact]
    public void ConfigPage_DoesNotUseTemplatePlaceholders()
    {
        string contents = ReadConfigPage();

        Assert.DoesNotContain("${", contents, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("id=\"SmtpPort\"", "value=\"587\"")]
    [InlineData("id=\"KccExecutable\"", "value=\"kcc-c2e\"")]
    [InlineData("id=\"CalibreExecutable\"", "value=\"ebook-convert\"")]
    [InlineData("id=\"AttachmentLimitMegabytes\"", "value=\"18\"")]
    [InlineData("id=\"ConversionTimeoutMinutes\"", "value=\"30\"")]
    [InlineData("id=\"KccProfile\"", "value=\"KV\"")]
    [InlineData("id=\"KccGamma\"", "value=\"Auto\"")]
    [InlineData("id=\"KccCropping\"", "value=\"2\"")]
    [InlineData("id=\"KccCroppingPower\"", "value=\"1\"")]
    [InlineData("id=\"KccJpegQuality\"", "value=\"85\"")]
    public void ConfigPage_ProvidesInitialFieldDefault(string fieldMarker, string defaultMarker)
    {
        string contents = ReadConfigPage();
        int fieldStart = contents.IndexOf(fieldMarker, StringComparison.Ordinal);

        Assert.True(fieldStart >= 0, $"Field marker '{fieldMarker}' was not found.");
        int fieldEnd = contents.IndexOf("/>", fieldStart, StringComparison.Ordinal);
        Assert.True(fieldEnd > fieldStart, $"Field '{fieldMarker}' did not end with '/>'.");
        Assert.Contains(defaultMarker, contents[fieldStart..fieldEnd], StringComparison.Ordinal);
    }

    [Fact]
    public void ConfigPage_SelectsStartTlsByDefault()
    {
        string contents = ReadConfigPage();

        Assert.Contains("<option value=\"1\" selected>STARTTLS</option>", contents, StringComparison.Ordinal);
    }

    private static string ReadConfigPage()
    {
        Assembly assembly = typeof(Plugin).Assembly;
        using Stream stream = assembly.GetManifestResourceStream(
            "Jellyfin.Plugin.SendToKindle.Configuration.configPage.html")
            ?? throw new InvalidOperationException("Embedded configuration page was not found.");
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}
