using NetPdf.Fluent;
using NetPdf.Layout;
using Xunit;

namespace NetPdf.Tests.Core;

public class LeanCoreTests
{
    // A 1x1 PNG.
    private static readonly byte[] OnePixelPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private static readonly string[] HeavyPrefixes = ["SkiaSharp", "Svg.", "ZXing", "PDFtoImage"];

    [Fact]
    public void Core_BuildsTextAndImagePdf_WithoutAddOns()
    {
        var bytes = Document.Create(doc => doc.Page(page => page.Content(content =>
            content.Column(column =>
            {
                column.Item().Text("Lean core works");
                column.Item().Width(50).Image(ImageSource.FromBytes(OnePixelPng));
            })))).ToBytes();

        using var pdf = PdfFile.Open(bytes);
        Assert.Equal(1, pdf.PageCount);
        Assert.Contains("Lean core works", pdf.ExtractText());
        Assert.NotEmpty(pdf.GetImages(0));
    }

    [Fact]
    public void Core_DoesNotReferenceHeavyLibraries()
    {
        var referenced = typeof(PdfFile).Assembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? "")
            .Where(n => HeavyPrefixes.Any(p => n.StartsWith(p, StringComparison.OrdinalIgnoreCase)));

        Assert.Empty(referenced);
    }

    [Fact]
    public void Core_DoesNotShipHeavyLibrariesInOutput()
    {
        var dir = AppContext.BaseDirectory;
        var heavy = Directory.EnumerateFiles(dir, "*.dll")
            .Select(Path.GetFileName)
            .Where(n => n is not null && HeavyPrefixes.Any(p => n.StartsWith(p, StringComparison.OrdinalIgnoreCase)));

        Assert.Empty(heavy);
    }
}
