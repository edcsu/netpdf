using NetPdf.Fluent;
using NetPdf.Layout;
using Xunit;

namespace NetPdf.Tests;

public class SvgTests
{
    private const string RectSvg =
        """<svg xmlns="http://www.w3.org/2000/svg" width="40" height="20"><rect width="40" height="20" fill="red"/></svg>""";

    [Fact]
    public void FromSvg_ProducesPngBytes()
    {
        var source = ImageSource.FromSvg(RectSvg);
        Assert.Equal([0x89, 0x50, 0x4E, 0x47], source.Data.Take(4));
    }

    [Fact]
    public void FromSvg_InvalidMarkup_Throws()
    {
        Assert.Throws<ArgumentException>(() => ImageSource.FromSvg("not svg at all <"));
    }

    [Fact]
    public void FromSvg_ZeroScale_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ImageSource.FromSvg(RectSvg, 0));
    }

    [Fact]
    public void FromSvg_ScaleGrowsOutput()
    {
        var small = ImageSource.FromSvg(RectSvg, 1);
        var big = ImageSource.FromSvg(RectSvg, 4);
        Assert.True(big.Data.Length >= small.Data.Length);
    }

    [Fact]
    public void Svg_RendersInRealDocument()
    {
        var bytes = Document.Create(doc => doc.Page(page =>
            page.Content(c => c.Width(80).Svg(RectSvg)))).ToBytes();

        using var pdf = PdfFile.Open(bytes);
        Assert.Equal(1, pdf.PageCount);
        Assert.NotEmpty(pdf.GetImages(0));
    }
}
