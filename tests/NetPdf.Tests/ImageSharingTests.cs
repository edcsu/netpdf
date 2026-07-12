using NetPdf.Layout;
using PdfSharp.Drawing;
using SharpDocument = PdfSharp.Pdf.PdfDocument;
using Xunit;

namespace NetPdf.Tests;

public class ImageSharingTests
{
    // 1x1 transparent PNG.
    private static readonly byte[] OnePixelPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    [Fact]
    public void SameImageSourceInstance_IsDecodedOnce()
    {
        using var document = new SharpDocument();
        var page = document.AddPage();
        using var gfx = XGraphics.FromPdfPage(page);
        var canvas = new PdfSharpCanvas(gfx);

        var shared = ImageSource.FromBytes(OnePixelPng);
        for (var i = 0; i < 5; i++)
            canvas.DrawImage(shared, i * 10, 0, 8, 8);

        Assert.Equal(1, canvas.CachedImageCount);
    }

    [Fact]
    public void DistinctImageSourceInstances_AreDecodedSeparately()
    {
        using var document = new SharpDocument();
        var page = document.AddPage();
        using var gfx = XGraphics.FromPdfPage(page);
        var canvas = new PdfSharpCanvas(gfx);

        // Same bytes, different instances: caching is per instance, not per content.
        canvas.DrawImage(ImageSource.FromBytes(OnePixelPng), 0, 0, 8, 8);
        canvas.DrawImage(ImageSource.FromBytes(OnePixelPng), 10, 0, 8, 8);

        Assert.Equal(2, canvas.CachedImageCount);
    }
}
