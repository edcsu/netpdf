namespace NetPdf.Layout.Elements;

/// <summary>
/// A single line of text with page-number placeholders resolved at draw time: <c>{number}</c>
/// becomes the current page and <c>{total}</c> the total page count. <c>{total}</c> renders as
/// <c>?</c> when the total is not known (single-pass rendering).
/// </summary>
public sealed class PageNumberText : IElement
{
    private readonly string _format;
    private readonly TextStyle _style;

    /// <summary>Creates the element with a format string (e.g. <c>"Page {number} of {total}"</c>) and style.</summary>
    public PageNumberText(string format, TextStyle? style = null)
    {
        ArgumentNullException.ThrowIfNull(format);
        _format = format;
        _style = style ?? new TextStyle();
    }

    /// <inheritdoc />
    public SpacePlan Measure(ICanvas canvas, Size availableSpace)
    {
        var size = canvas.MeasureText(Format(canvas.PageContext), _style);
        return size.Width > availableSpace.Width || size.Height > availableSpace.Height
            ? SpacePlan.Wrap()
            : SpacePlan.FullRender(size);
    }

    /// <inheritdoc />
    public void Draw(ICanvas canvas, Size availableSpace) =>
        canvas.DrawText(Format(canvas.PageContext), _style, 0, 0);

    private string Format(PageContext context) => _format
        .Replace("{number}", context.CurrentPage.ToString())
        .Replace("{total}", context.TotalPages > 0 ? context.TotalPages.ToString() : "?");
}
