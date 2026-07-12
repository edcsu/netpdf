using PdfSharp.Drawing;
using SharpDocument = PdfSharp.Pdf.PdfDocument;

namespace NetPdf.Layout;

/// <summary>Render loop that measures and draws an element tree, adding pages until the content is fully rendered.</summary>
internal static class LayoutRenderer
{
    private const int MaxPages = 1000;

    internal static void Render(SharpDocument document, IElement root,
        double pageWidth, double pageHeight, double margin)
    {
        var contentWidth = pageWidth - 2 * margin;
        var contentHeight = pageHeight - 2 * margin;
        if (contentWidth <= 0 || contentHeight <= 0)
            throw new LayoutException("The margins leave no room for content on the page.");
        var contentSize = new Size(contentWidth, contentHeight);

        for (var pageIndex = 0; pageIndex < MaxPages; pageIndex++)
        {
            var page = document.AddPage();
            page.Width = XUnit.FromPoint(pageWidth);
            page.Height = XUnit.FromPoint(pageHeight);

            using var gfx = XGraphics.FromPdfPage(page);
            var canvas = new PdfSharpCanvas(gfx);
            canvas.Translate(margin, margin);

            var plan = root.Measure(canvas, contentSize);
            switch (plan.Type)
            {
                case SpacePlanType.FullRender:
                    root.Draw(canvas, contentSize);
                    return;

                case SpacePlanType.PartialRender:
                    if (plan.Size.Height <= 0)
                        throw new LayoutException(
                            "An element reported a partial render without consuming any space; the document would never finish.");
                    root.Draw(canvas, contentSize);
                    break;

                case SpacePlanType.Wrap:
                    throw new LayoutException("The content does not fit on an empty page.");
            }
        }

        throw new LayoutException($"The content did not finish rendering within {MaxPages} pages.");
    }
}
