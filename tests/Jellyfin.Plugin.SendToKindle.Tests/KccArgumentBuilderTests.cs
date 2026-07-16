using Jellyfin.Plugin.SendToKindle.Configuration;
using Jellyfin.Plugin.SendToKindle.Conversion;
using Jellyfin.Plugin.SendToKindle.Jobs;

namespace Jellyfin.Plugin.SendToKindle.Tests;

public sealed class KccArgumentBuilderTests
{
    [Fact]
    public void Build_ForcesManagedOutputAndSplittingOptions()
    {
        KccArgumentBuilder builder = new(new AdvancedArgumentParser());
        PluginConfiguration configuration = new()
        {
            AttachmentLimitMegabytes = 18,
            Kcc = new KccOptions
            {
                Profile = "KPW5",
                MangaStyle = true,
                AdvancedArguments = "--forcepng",
            },
        };
        BookSource source = new(Guid.NewGuid(), "/library/My Comic.cbz", "My Comic", "A. Writer");

        IReadOnlyList<string> result = builder.Build(source, "/tmp/output", configuration);

        Assert.Contains("--manga-style", result);
        Assert.Contains("--forcepng", result);
        AssertOption(result, "--format", "EPUB");
        AssertOption(result, "--batchsplit", "1");
        AssertOption(result, "--targetsize", "18");
        AssertOption(result, "--output", "/tmp/output");
        Assert.Equal(source.Path, result[^1]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Auto")]
    [InlineData("auto")]
    public void Build_OmitsGammaForAutomaticMode(string gamma)
    {
        KccArgumentBuilder builder = new(new AdvancedArgumentParser());
        PluginConfiguration configuration = new()
        {
            Kcc = new KccOptions { Gamma = gamma },
        };

        IReadOnlyList<string> result = builder.Build(
            new BookSource(Guid.NewGuid(), "/library/book.cbz", "Book", "Author"),
            "/tmp/output",
            configuration);

        Assert.DoesNotContain("--gamma", result);
    }

    [Fact]
    public void Build_AddsNumericGamma()
    {
        KccArgumentBuilder builder = new(new AdvancedArgumentParser());
        PluginConfiguration configuration = new()
        {
            Kcc = new KccOptions { Gamma = "1.25" },
        };

        IReadOnlyList<string> result = builder.Build(
            new BookSource(Guid.NewGuid(), "/library/book.cbz", "Book", "Author"),
            "/tmp/output",
            configuration);

        AssertOption(result, "--gamma", "1.25");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("0")]
    [InlineData("-1")]
    public void Build_RejectsInvalidGamma(string gamma)
    {
        KccArgumentBuilder builder = new(new AdvancedArgumentParser());
        PluginConfiguration configuration = new()
        {
            Kcc = new KccOptions { Gamma = gamma },
        };

        ArgumentException exception = Assert.Throws<ArgumentException>(() => builder.Build(
            new BookSource(Guid.NewGuid(), "/library/book.cbz", "Book", "Author"),
            "/tmp/output",
            configuration));

        Assert.Contains("gamma", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertOption(IReadOnlyList<string> arguments, string option, string expectedValue)
    {
        int index = arguments.ToList().IndexOf(option);
        Assert.True(index >= 0, $"Option {option} was not present.");
        Assert.True(index + 1 < arguments.Count, $"Option {option} had no value.");
        Assert.Equal(expectedValue, arguments[index + 1]);
    }
}
