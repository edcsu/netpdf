using System.Runtime.Versioning;
using PDFtoImage;
using SkiaSharp;

namespace NetPdf.Rendering;

/// <summary>Renders PDF pages to PNG bitmaps using PDFium via PDFtoImage.</summary>
internal static class PdfRenderer
{
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst13.5")]
    [SupportedOSPlatform("android31.0")]
    [SupportedOSPlatform("ios13.6")]
    internal static byte[] RenderPage(byte[] pdf, int pageIndex, int dpi, string? password = null)
    {
        var options = new RenderOptions(Dpi: dpi);
        using SKBitmap bitmap = Conversion.ToImage(pdf, password: password, page: pageIndex, options: options);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, quality: 100);
        return data.ToArray();
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst13.5")]
    [SupportedOSPlatform("android31.0")]
    [SupportedOSPlatform("ios13.6")]
    internal static int GetPageCount(byte[] pdf, string? password = null) =>
        Conversion.GetPageCount(pdf, password: password);
}
