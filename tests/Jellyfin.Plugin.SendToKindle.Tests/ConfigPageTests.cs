using System.Reflection;

namespace Jellyfin.Plugin.SendToKindle.Tests;

public sealed class ConfigPageTests
{
    [Fact]
    public void ConfigPage_DoesNotUseTemplatePlaceholders()
    {
        Assembly assembly = typeof(Plugin).Assembly;
        using Stream stream = assembly.GetManifestResourceStream(
            "Jellyfin.Plugin.SendToKindle.Configuration.configPage.html")
            ?? throw new InvalidOperationException("Embedded configuration page was not found.");
        using StreamReader reader = new(stream);

        string contents = reader.ReadToEnd();

        Assert.DoesNotContain("${", contents, StringComparison.Ordinal);
    }
}
