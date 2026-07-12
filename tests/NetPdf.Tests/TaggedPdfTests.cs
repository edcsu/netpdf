using System.Text;
using NetPdf.Fluent;
using NetPdf.Layout;
using Xunit;

namespace NetPdf.Tests;

public class TaggedPdfTests
{
    private static byte[] BuildTaggedPdf() =>
        Document.Create(doc => doc
                .Page(page => page.Content(c => c.Column(col =>
                {
                    col.Item().Heading(1).Text("Title");
                    col.Item().Paragraph().Text("Body paragraph");
                }))))
            .WithTagging()
            .ToBytes();

    [Fact]
    public void WithTagging_SetsMarkInfoAndStructTreeRoot()
    {
        var text = Encoding.Latin1.GetString(BuildTaggedPdf());
        Assert.Contains("/MarkInfo", text);
        Assert.Contains("/Marked true", text);
        Assert.Contains("/StructTreeRoot", text);
        Assert.Contains("/ParentTree", text);
    }

    [Fact]
    public void WithTagging_EmitsMarkedContentOperators()
    {
        var bytes = BuildTaggedPdf();

        // The content stream may be stored compressed; unfilter it to inspect operators.
        var doc = PdfSharp.Pdf.IO.PdfReader.Open(new MemoryStream(bytes),
            PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
        var content = doc.Pages[0].Contents.Elements.GetDictionary(0)!;
        content.Stream.TryUnfilter();
        var text = Encoding.Latin1.GetString(content.Stream.Value);
        Assert.Contains("/H1 <</MCID 0>> BDC", text);
        Assert.Contains("/P <</MCID 1>> BDC", text);
        Assert.Contains("EMC", text);
        Assert.DoesNotContain("MCB", text);
    }

    [Fact]
    public void WithTagging_StructureElementsCarryRoles()
    {
        var text = Encoding.Latin1.GetString(BuildTaggedPdf());
        Assert.Contains("/S/H1", text.Replace(" ", ""));
        Assert.Contains("/S/P", text.Replace(" ", ""));
        Assert.Contains("/S/Document", text.Replace(" ", ""));
    }

    [Fact]
    public void WithTagging_FigureCarriesAltText()
    {
        var png = Creation.BarcodeGenerator.GenerateQrCode("alt-test", 64);
        var bytes = Document.Create(doc => doc
                .Page(page => page.Content(c => c.Width(50).Image(png, altText: "A QR code"))))
            .WithTagging()
            .ToBytes();

        var text = Encoding.Latin1.GetString(bytes);
        Assert.Contains("/S/Figure", text.Replace(" ", ""));
        Assert.Contains("A QR code", text);
    }

    [Fact]
    public void WithTagging_ContentStillExtractsAndRenders()
    {
        using var pdf = PdfFile.Open(BuildTaggedPdf());
        Assert.Contains("Title", pdf.ExtractText());
        Assert.Contains("Body paragraph", pdf.ExtractText());
        Assert.True(pdf.RenderPage(0).Length > 0);
    }

    [Fact]
    public void WithoutTagging_SemanticRolesAreNoOps()
    {
        var bytes = Document.Create(doc => doc
                .Page(page => page.Content(c => c.Heading(1).Text("Untagged"))))
            .ToBytes();

        var text = Encoding.Latin1.GetString(bytes);
        Assert.DoesNotContain("/StructTreeRoot", text);
        using var pdf = PdfFile.Open(bytes);
        Assert.Contains("Untagged", pdf.ExtractText());
    }

    [Fact]
    public void TaggingAndPdfA_Combine()
    {
        var bytes = Document.Create(doc => doc
                .Page(page => page.Content(c => c.Paragraph().Text("Both"))))
            .WithTagging()
            .AsPdfA()
            .ToBytes();

        var text = Encoding.Latin1.GetString(bytes);
        Assert.Contains("/StructTreeRoot", text);
        Assert.Contains("/GTS_PDFA1", text);
        using var pdf = PdfFile.Open(bytes);
        Assert.Contains("Both", pdf.ExtractText());
    }
}
