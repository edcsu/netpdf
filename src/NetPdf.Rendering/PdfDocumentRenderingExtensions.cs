using System.Runtime.Versioning;
using NetPdf.Rendering;

// Same namespace as PdfDocument, so callers need no extra "using".
namespace NetPdf;

/// <summary>Page rendering for <see cref="PdfDocument"/>, provided by NetPdfKit.Rendering.</summary>
public static class PdfDocumentRenderingExtensions
{
    /// <summary>Renders a page (0-based index) to a PNG image.</summary>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst13.5")]
    [SupportedOSPlatform("android31.0")]
    [SupportedOSPlatform("ios13.6")]
    public static byte[] RenderPage(this PdfDocument document, int pageIndex, int dpi = 150)
    {
        ArgumentNullException.ThrowIfNull(document);
        return PdfRenderer.RenderPage(document.RawBytes, pageIndex, dpi, document.Password);
    }

    /// <summary>Renders every page to a PNG image.</summary>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst13.5")]
    [SupportedOSPlatform("android31.0")]
    [SupportedOSPlatform("ios13.6")]
    public static IReadOnlyList<byte[]> RenderAllPages(this PdfDocument document, int dpi = 150)
    {
        ArgumentNullException.ThrowIfNull(document);
        var count = PdfRenderer.GetPageCount(document.RawBytes, document.Password);
        var pages = new List<byte[]>(count);
        for (var i = 0; i < count; i++)
            pages.Add(document.RenderPage(i, dpi));
        return pages;
    }
}
