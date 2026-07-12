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
        var layout = new PageLayout
        {
            PageWidth = pageWidth,
            PageHeight = pageHeight,
            MarginLeft = margin,
            MarginTop = margin,
            MarginRight = margin,
            MarginBottom = margin,
            Content = () => root,
        };
        Render(document, layout, new PageContext());
    }

    /// <summary>
    /// Renders a page layout: the header and footer factories are invoked and drawn on every
    /// page (each must fully fit), and the content element flows through the space between
    /// them until it is fully rendered. Returns the number of pages added.
    /// </summary>
    internal static int Render(SharpDocument document, PageLayout layout, PageContext context)
    {
        var contentWidth = layout.PageWidth - layout.MarginLeft - layout.MarginRight;
        var contentHeight = layout.PageHeight - layout.MarginTop - layout.MarginBottom;
        if (contentWidth <= 0 || contentHeight <= 0)
            throw new LayoutException("The margins leave no room for content on the page.");

        var content = layout.Content();
        for (var pageIndex = 0; pageIndex < MaxPages; pageIndex++)
        {
            context.CurrentPage++;
            var page = document.AddPage();
            page.Width = XUnit.FromPoint(layout.PageWidth);
            page.Height = XUnit.FromPoint(layout.PageHeight);

            using var gfx = XGraphics.FromPdfPage(page);
            var canvas = new PdfSharpCanvas(gfx, context, page);
            canvas.Translate(layout.MarginLeft, layout.MarginTop);

            var slotSize = new Size(contentWidth, contentHeight);
            var headerHeight = DrawSlot(canvas, layout.Header?.Invoke(), slotSize, dy: 0, "header");

            var footer = layout.Footer?.Invoke();
            var footerHeight = MeasureSlot(canvas, footer, slotSize, "footer");
            if (footer is not null)
                DrawSlot(canvas, footer, slotSize, dy: contentHeight - footerHeight, "footer");

            var bodyHeight = contentHeight - headerHeight - footerHeight;
            if (bodyHeight <= 0)
                throw new LayoutException("The header and footer leave no room for content on the page.");

            canvas.Translate(0, headerHeight);
            var bodySize = new Size(contentWidth, bodyHeight);
            var plan = content.Measure(canvas, bodySize);
            switch (plan.Type)
            {
                case SpacePlanType.FullRender:
                    content.Draw(canvas, bodySize);
                    return pageIndex + 1;

                case SpacePlanType.PartialRender:
                    if (plan.Size.Height <= 0)
                        throw new LayoutException(
                            "An element reported a partial render without consuming any space; the document would never finish.");
                    content.Draw(canvas, bodySize);
                    break;

                case SpacePlanType.Wrap:
                    throw new LayoutException("The content does not fit on an empty page.");
            }
        }

        throw new LayoutException($"The content did not finish rendering within {MaxPages} pages.");
    }

    // Measures a repeated slot (must fully fit) and draws it at the given vertical offset; returns its height.
    private static double DrawSlot(ICanvas canvas, IElement? element, Size slotSize, double dy, string slotName)
    {
        var height = MeasureSlot(canvas, element, slotSize, slotName);
        if (element is not null)
        {
            canvas.Save();
            canvas.Translate(0, dy);
            element.Draw(canvas, slotSize);
            canvas.Restore();
        }
        return height;
    }

    private static double MeasureSlot(ICanvas canvas, IElement? element, Size slotSize, string slotName)
    {
        if (element is null)
            return 0;

        var plan = element.Measure(canvas, slotSize);
        if (plan.Type != SpacePlanType.FullRender)
            throw new LayoutException($"The {slotName} does not fit on the page.");
        return plan.Size.Height;
    }
}
