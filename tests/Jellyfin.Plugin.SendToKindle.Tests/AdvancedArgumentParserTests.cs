using Jellyfin.Plugin.SendToKindle.Conversion;

namespace Jellyfin.Plugin.SendToKindle.Tests;

public sealed class AdvancedArgumentParserTests
{
    private readonly AdvancedArgumentParser _parser = new();

    [Fact]
    public void Parse_AllowsKnownOptionsAndQuotedValues()
    {
        IReadOnlyList<string> result = _parser.Parse("--forcepng --preservemargin '12 px'");

        Assert.Equal(new[] { "--forcepng", "--preservemargin", "12 px" }, result);
    }

    [Theory]
    [InlineData("--output /tmp/elsewhere")]
    [InlineData("--unknown")]
    [InlineData("book.cbz")]
    [InlineData("--forcepng=true")]
    public void Parse_RejectsUnsafeOrMalformedOptions(string arguments)
    {
        Assert.Throws<ArgumentException>(() => _parser.Parse(arguments));
    }

    [Theory]
    [InlineData("--preservemargin")]
    [InlineData("--preservemargin --forcepng")]
    [InlineData("--preservemargin 'unterminated")]
    public void Parse_RejectsMissingValuesAndIncompleteQuotes(string arguments)
    {
        Assert.Throws<ArgumentException>(() => _parser.Parse(arguments));
    }
}
