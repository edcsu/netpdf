using NetPdf.Creation;
using Xunit;

namespace NetPdf.Tests;

public class PageBuilderTests
{
    [Fact]
    public void Drawing_primitives_produce_a_valid_single_page_pdf()
    {
        var bytes = PdfFile.Create()
            .AddPage(p => p
                .AddEllipse(50, 50, 120, 80, fill: System.Drawing.Color.LightBlue)
                .AddPolygon([(200, 50), (260, 130), (140, 130)], fill: System.Drawing.Color.LightGreen)
                .AddBezier(50, 200, 100, 150, 200, 250, 250, 200)
                .AddRoundedRectangle(50, 280, 150, 60, cornerRadius: 10,
                    fill: System.Drawing.Color.LightGray))
            .ToBytes();

        using var doc = PdfFile.Open(bytes);
        Assert.Equal(1, doc.PageCount);
    }

    [Fact]
    public void Polygon_requires_at_least_three_points()
    {
        Assert.Throws<ArgumentException>(() =>
            PdfFile.Create().AddPage(p => p.AddPolygon([(0, 0), (10, 10)])).ToBytes());
    }

    [Fact]
    public void Text_styles_and_alignment_round_trip()
    {
        var bytes = PdfFile.Create()
            .AddPage(p => p
                .AddText("underlined", 50, 50, o => o.Underline())
                .AddText("struck", 50, 80, o => o.Strikethrough())
                .AddText("centered text", 50, 110, o => o.Align(TextAlignment.Center))
                .AddText("right aligned wrapped text", 50, 140, o => o.Align(TextAlignment.Right).Wrap(200)))
            .ToBytes();

        using var doc = PdfFile.Open(bytes);
        // XTextFormatter draws word-by-word, so extracted text loses spaces; assert word survival.
        var text = doc.ExtractText();
        foreach (var word in new[] { "underlined", "struck", "centered", "aligned", "wrapped" })
            Assert.Contains(word, text);
    }

    [Fact]
    public void Line_spacing_keeps_all_wrapped_text()
    {
        const string longText = "The quick brown fox jumps over the lazy dog again and again until the line wraps.";
        var bytes = PdfFile.Create()
            .AddPage(p => p.AddText(longText, 50, 50, o => o.Wrap(200).LineSpacing(1.5)))
            .ToBytes();

        using var doc = PdfFile.Open(bytes);
        // Extraction normalizes whitespace differently; assert word survival.
        foreach (var word in new[] { "quick", "jumps", "wraps." })
            Assert.Contains(word, doc.ExtractText());
    }

    [Fact]
    public void Line_spacing_must_be_positive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PdfFile.Create().AddPage(p => p.AddText("x", 0, 0, o => o.LineSpacing(0))).ToBytes());
    }
}
