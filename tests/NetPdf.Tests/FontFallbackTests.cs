using NetPdf.Creation;
using PdfSharp.Fonts;
using Xunit;

namespace NetPdf.Tests;

public class FontFallbackTests
{
    // A resolver that sees no OS fonts, like a bare container.
    private static SystemFontResolver NoFonts() => new(Array.Empty<string>());

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void Unknown_font_with_no_os_fonts_uses_bundled_font(bool bold, bool italic)
    {
        var resolver = NoFonts();

        var info = resolver.ResolveTypeface("Arial", bold, italic);

        Assert.NotNull(info);
        var bytes = resolver.GetFont(info!.FaceName);
        Assert.NotNull(bytes);
        Assert.True(bytes!.Length > 100_000);
        // TrueType files start with 0x00010000.
        Assert.Equal(new byte[] { 0, 1, 0, 0 }, bytes[..4]);
    }

    [Fact]
    public void Registered_font_wins_over_fallback()
    {
        var data = new byte[] { 0, 1, 0, 0, 42 };
        NetPdfFonts.Register("Test Brand Font", data);
        var resolver = NoFonts();

        var info = resolver.ResolveTypeface("Test Brand Font", bold: true, italic: false);

        Assert.NotNull(info);
        Assert.Equal(data, resolver.GetFont(info!.FaceName));
    }

    [Fact]
    public void Resolver_never_returns_null()
    {
        Assert.NotNull(NoFonts().ResolveTypeface("Does Not Exist", false, false));
    }
}
