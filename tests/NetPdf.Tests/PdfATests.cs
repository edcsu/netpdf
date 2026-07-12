using System.Text;
using NetPdf.Creation;
using NetPdf.Fluent;
using Xunit;

namespace NetPdf.Tests;

public class PdfATests
{
    private static byte[] BuildPdfA() =>
        Document.Create(doc => doc
                .Page(page => page.Content(c => c.Text("Conforming content"))))
            .AsPdfA()
            .ToBytes();

    [Fact]
    public void AsPdfA_EmbedsOutputIntent()
    {
        var text = Encoding.Latin1.GetString(BuildPdfA());
        Assert.Contains("/OutputIntents", text);
        Assert.Contains("/GTS_PDFA1", text);
        Assert.Contains("sRGB IEC61966-2.1", text);
    }

    [Fact]
    public void AsPdfA_WritesPdfAIdentificationXmp()
    {
        using var pdf = PdfFile.Open(BuildPdfA());
        var xmp = pdf.GetXmpMetadata();
        Assert.NotNull(xmp);
        Assert.Contains("<pdfaid:part>2</pdfaid:part>", xmp);
        Assert.Contains("<pdfaid:conformance>B</pdfaid:conformance>", xmp);
    }

    [Fact]
    public void AsPdfA_EmbedsFonts()
    {
        var text = Encoding.Latin1.GetString(BuildPdfA());
        Assert.Contains("/FontFile2", text);
    }

    [Fact]
    public void AsPdfA_ContentSurvives()
    {
        using var pdf = PdfFile.Open(BuildPdfA());
        Assert.Equal(1, pdf.PageCount);
        Assert.Contains("Conforming content", pdf.ExtractText());
        Assert.True(pdf.RenderPage(0).Length > 0);
    }

    [Fact]
    public void GeneratedIccProfile_IsStructurallyValid()
    {
        var icc = PdfAConformance.BuildSrgbProfile();

        // Size field matches, signature 'acsp' at offset 36, display class 'mntr' at 12.
        var size = (icc[0] << 24) | (icc[1] << 16) | (icc[2] << 8) | icc[3];
        Assert.Equal(icc.Length, size);
        Assert.Equal("mntr", Encoding.ASCII.GetString(icc, 12, 4));
        Assert.Equal("RGB ", Encoding.ASCII.GetString(icc, 16, 4));
        Assert.Equal("acsp", Encoding.ASCII.GetString(icc, 36, 4));
    }
}
