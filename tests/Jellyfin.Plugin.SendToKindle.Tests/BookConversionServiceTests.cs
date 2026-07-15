using Jellyfin.Plugin.SendToKindle.Conversion;

namespace Jellyfin.Plugin.SendToKindle.Tests;

public sealed class BookConversionServiceTests
{
    [Theory]
    [InlineData("book.epub")]
    [InlineData("book.PDF")]
    [InlineData("book.mobi")]
    [InlineData("book.azw")]
    [InlineData("book.azw3")]
    [InlineData("book.cbr")]
    [InlineData("book.CBZ")]
    public void IsSupportedExtension_AcceptsConfiguredFormats(string path)
    {
        Assert.True(BookConversionService.IsSupportedExtension(path));
    }

    [Theory]
    [InlineData("book.txt")]
    [InlineData("book.docx")]
    [InlineData("book")]
    public void IsSupportedExtension_RejectsOtherFormats(string path)
    {
        Assert.False(BookConversionService.IsSupportedExtension(path));
    }
}
