using System.Text;
using NetPdf.Fluent;
using Xunit;

namespace NetPdf.Tests;

public class LinearizationTests
{
    private static byte[] BuildMultiPagePdf() =>
        Document.Create(doc => doc
                .Page(page => page.Content(c => c.Text("First page marker")))
                .Page(page => page.Content(c => c.Text("Second page marker")))
                .Page(page => page.Content(c => c.Text("Third page marker"))))
            .ToBytes();

    [Fact]
    public void Linearize_PutsLinearizationDictFirst()
    {
        using var pdf = PdfFile.Open(BuildMultiPagePdf());
        var bytes = pdf.Linearize().ToBytes();

        var head = Encoding.Latin1.GetString(bytes, 0, Math.Min(1024, bytes.Length));
        Assert.Contains("/Linearized 1", head);
    }

    [Fact]
    public void Linearize_LengthEntryMatchesFileLength()
    {
        using var pdf = PdfFile.Open(BuildMultiPagePdf());
        var bytes = pdf.Linearize().ToBytes();

        var head = Encoding.Latin1.GetString(bytes, 0, 1024);
        var match = System.Text.RegularExpressions.Regex.Match(head, @"/L\s+(\d+)");
        Assert.True(match.Success);
        Assert.Equal(bytes.Length, long.Parse(match.Groups[1].Value));
    }

    [Fact]
    public void Linearize_HasTwoCrossReferenceSections()
    {
        using var pdf = PdfFile.Open(BuildMultiPagePdf());
        var text = Encoding.Latin1.GetString(pdf.Linearize().ToBytes());

        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(text, @"(?<!start)xref\b").Count);
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(text, @"startxref").Count);
    }

    [Fact]
    public void Linearize_FirstPageObjectsPrecedeMainSection()
    {
        using var pdf = PdfFile.Open(BuildMultiPagePdf());
        var text = Encoding.Latin1.GetString(pdf.Linearize().ToBytes());

        // The catalog (/Type /Catalog) belongs to the first-page section and must appear
        // in the file before the second startxref (the main xref lives at the end).
        var catalog = System.Text.RegularExpressions.Regex.Match(text, @"/Type\s*/Catalog");
        var pages = System.Text.RegularExpressions.Regex.Matches(text, @"/Type\s*/Page[^s]");
        Assert.True(catalog.Success);
        Assert.True(pages.Count >= 3);
        Assert.True(catalog.Index < pages[^1].Index,
            "catalog must precede the later pages' objects");
    }

    [Fact]
    public void Linearize_RoundTripPreservesContent()
    {
        using var pdf = PdfFile.Open(BuildMultiPagePdf());
        using var linearized = PdfFile.Open(pdf.Linearize().ToBytes());

        Assert.Equal(3, linearized.PageCount);
        Assert.Contains("First page marker", linearized.ExtractText(0));
        Assert.Contains("Second page marker", linearized.ExtractText(1));
        Assert.Contains("Third page marker", linearized.ExtractText(2));
    }

    [Fact]
    public void Linearize_OutputRendersWithPdfium()
    {
        using var pdf = PdfFile.Open(BuildMultiPagePdf());
        using var linearized = PdfFile.Open(pdf.Linearize().ToBytes());

        Assert.True(linearized.RenderPage(0).Length > 0);
        Assert.True(linearized.RenderPage(2).Length > 0);
    }

    [Fact]
    public void Linearize_DocumentWithImagesAndTables_Survives()
    {
        var bytes = Document.Create(doc => doc
                .Page(page => page.Content(c => c.Table(t =>
                {
                    t.ColumnsDefinition(cols => { cols.RelativeColumn(); cols.RelativeColumn(); });
                    t.Cell().Text("A1");
                    t.Cell().Text("B1");
                })))
                .Page(page => page.Content(c => c.Text("Tail"))))
            .ToBytes();

        using var pdf = PdfFile.Open(bytes);
        using var linearized = PdfFile.Open(pdf.Linearize().ToBytes());
        Assert.Equal(2, linearized.PageCount);
        Assert.Contains("A1", linearized.ExtractText(0));
        Assert.True(linearized.RenderPage(0).Length > 0);
    }
}
