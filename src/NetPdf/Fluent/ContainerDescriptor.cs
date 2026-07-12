using NetPdf.Layout;
using NetPdf.Layout.Elements;

namespace NetPdf.Fluent;

/// <summary>
/// Configures one content slot. Chainable calls wrap the slot in sizing/position containers;
/// terminal calls (<see cref="Text(string, TextStyle?)"/>, <see cref="Column"/>, …) place the slot's content.
/// </summary>
public sealed class ContainerDescriptor
{
    private readonly Action<IElement> _assign;

    internal ContainerDescriptor(Action<IElement> assign) => _assign = assign;

    private ContainerDescriptor Wrap(ContainerElement wrapper)
    {
        _assign(wrapper);
        return new ContainerDescriptor(child => wrapper.Child = child);
    }

    /// <summary>Insets the content by the same padding on all sides.</summary>
    public ContainerDescriptor Padding(double all) => Padding(all, all, all, all);

    /// <summary>Insets the content by individual paddings per side.</summary>
    public ContainerDescriptor Padding(double left, double top, double right, double bottom) =>
        Wrap(new PaddingElement { Left = left, Top = top, Right = right, Bottom = bottom });

    /// <summary>Forces an exact width in points.</summary>
    public ContainerDescriptor Width(double value) => Wrap(new ConstrainedElement { MinWidth = value, MaxWidth = value });

    /// <summary>Forces an exact height in points.</summary>
    public ContainerDescriptor Height(double value) => Wrap(new ConstrainedElement { MinHeight = value, MaxHeight = value });

    /// <summary>Applies a minimum width in points.</summary>
    public ContainerDescriptor MinWidth(double value) => Wrap(new ConstrainedElement { MinWidth = value });

    /// <summary>Applies a maximum width in points.</summary>
    public ContainerDescriptor MaxWidth(double value) => Wrap(new ConstrainedElement { MaxWidth = value });

    /// <summary>Applies a minimum height in points.</summary>
    public ContainerDescriptor MinHeight(double value) => Wrap(new ConstrainedElement { MinHeight = value });

    /// <summary>Applies a maximum height in points.</summary>
    public ContainerDescriptor MaxHeight(double value) => Wrap(new ConstrainedElement { MaxHeight = value });

    /// <summary>Aligns the content to the left of the slot.</summary>
    public ContainerDescriptor AlignLeft() => Wrap(new AlignmentElement { Horizontal = HorizontalAlignment.Left });

    /// <summary>Centers the content horizontally.</summary>
    public ContainerDescriptor AlignCenter() => Wrap(new AlignmentElement { Horizontal = HorizontalAlignment.Center });

    /// <summary>Aligns the content to the right of the slot.</summary>
    public ContainerDescriptor AlignRight() => Wrap(new AlignmentElement { Horizontal = HorizontalAlignment.Right });

    /// <summary>Aligns the content to the top of the slot.</summary>
    public ContainerDescriptor AlignTop() => Wrap(new AlignmentElement { Vertical = VerticalAlignment.Top });

    /// <summary>Centers the content vertically.</summary>
    public ContainerDescriptor AlignMiddle() => Wrap(new AlignmentElement { Vertical = VerticalAlignment.Middle });

    /// <summary>Aligns the content to the bottom of the slot.</summary>
    public ContainerDescriptor AlignBottom() => Wrap(new AlignmentElement { Vertical = VerticalAlignment.Bottom });

    /// <summary>Sizes the content to a fixed width-to-height ratio.</summary>
    public ContainerDescriptor AspectRatio(double ratio, AspectRatioOption option = AspectRatioOption.FitArea) =>
        Wrap(new AspectRatioElement(ratio) { Option = option });

    /// <summary>Takes the full offered width and height.</summary>
    public ContainerDescriptor Extend() => Wrap(new ExtendElement());

    /// <summary>Takes the full offered width.</summary>
    public ContainerDescriptor ExtendHorizontal() => Wrap(new ExtendElement { ExtendVertical = false });

    /// <summary>Takes the full offered height.</summary>
    public ContainerDescriptor ExtendVertical() => Wrap(new ExtendElement { ExtendHorizontal = false });

    /// <summary>Occupies only the content's natural size.</summary>
    public ContainerDescriptor Shrink() => Wrap(new ShrinkElement());

    /// <summary>Measures the content with unlimited space, letting it overflow its slot.</summary>
    public ContainerDescriptor Unconstrained() => Wrap(new UnconstrainedElement());

    /// <summary>Shifts the content's drawing position without affecting layout.</summary>
    public ContainerDescriptor Offset(double x, double y) => Wrap(new OffsetElement { OffsetX = x, OffsetY = y });

    /// <summary>Paints a solid background behind the content.</summary>
    public ContainerDescriptor Background(System.Drawing.Color color, double cornerRadius = 0) =>
        Wrap(new BackgroundElement { Color = color, CornerRadius = cornerRadius });

    /// <summary>Strokes a uniform border on the content's edges; it consumes no layout space.</summary>
    public ContainerDescriptor Border(double thickness, System.Drawing.Color? color = null) =>
        Border(thickness, thickness, thickness, thickness, color);

    /// <summary>Strokes per-side borders on the content's edges; they consume no layout space.</summary>
    public ContainerDescriptor Border(double left, double top, double right, double bottom,
        System.Drawing.Color? color = null) =>
        Wrap(new BorderElement
        {
            Left = left,
            Top = top,
            Right = right,
            Bottom = bottom,
            Color = color ?? System.Drawing.Color.Black,
        });

    /// <summary>Keeps the content on one page, deferring it to the next page instead of splitting it.</summary>
    public ContainerDescriptor ShowEntire() => Wrap(new ShowEntireElement());

    /// <summary>
    /// Starts the content on the next page unless at least <paramref name="minHeight"/> points
    /// of height remain on the current page.
    /// </summary>
    public ContainerDescriptor EnsureSpace(double minHeight = 150) =>
        Wrap(new EnsureSpaceElement { MinHeight = minHeight });

    /// <summary>Renders the content only once; afterwards it occupies no space (for repeated slots).</summary>
    public ContainerDescriptor ShowOnce() => Wrap(new ShowOnceElement());

    /// <summary>Hides the content the first time this slot is rendered (for repeated slots).</summary>
    public ContainerDescriptor SkipOnce() => Wrap(new SkipOnceElement());

    /// <summary>Renders the content only when <paramref name="condition"/> is true.</summary>
    public ContainerDescriptor ShowIf(bool condition) => Wrap(new ShowIfElement { Condition = condition });

    /// <summary>Renders only what fits on the current page and discards the remainder.</summary>
    public ContainerDescriptor StopPaging() => Wrap(new StopPagingElement());

    /// <summary>Forces following content to start on the next page.</summary>
    public void PageBreak() => _assign(new PageBreakElement());

    /// <summary>
    /// Renders the described content again on every page while the surrounding context keeps
    /// paginating. Must be bounded (e.g. combined with <see cref="StopPaging"/>).
    /// </summary>
    public void Repeat(Action<ContainerDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _assign(new RepeatElement(() =>
        {
            IElement child = new EmptyElement();
            configure(new ContainerDescriptor(e => child = e));
            return child;
        }));
    }

    /// <summary>Applies a default text style to all text inside the slot.</summary>
    public ContainerDescriptor DefaultTextStyle(TextStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);
        return Wrap(new DefaultTextStyleElement { Style = style });
    }

    /// <summary>Places word-wrapping text in the slot.</summary>
    public void Text(string text, TextStyle? style = null) => _assign(new TextElement(text, style));

    /// <summary>Places a rich text block composed of styled spans in the slot.</summary>
    public void Text(Action<TextDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var descriptor = new TextDescriptor();
        configure(descriptor);
        _assign(descriptor.Build());
    }

    /// <summary>
    /// Places page-number text in the slot; <c>{number}</c> is the current page and
    /// <c>{total}</c> the total page count.
    /// </summary>
    public void PageNumber(string format = "{number}", TextStyle? style = null) =>
        _assign(new PageNumberText(format, style));

    /// <summary>Places an image in the slot, scaled to the available width.</summary>
    public void Image(ImageSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _assign(new ImageElement(source));
    }

    /// <summary>Places an image loaded from a file in the slot, scaled to the available width.</summary>
    public void Image(string filePath) => Image(ImageSource.FromFile(filePath));

    /// <summary>Places a horizontal rule spanning the available width.</summary>
    public void LineHorizontal(double thickness = 1, System.Drawing.Color? color = null) =>
        _assign(new LineElement
        {
            Orientation = LineOrientation.Horizontal,
            Thickness = thickness,
            Color = color ?? System.Drawing.Color.Black,
        });

    /// <summary>Places a vertical rule spanning the available height.</summary>
    public void LineVertical(double thickness = 1, System.Drawing.Color? color = null) =>
        _assign(new LineElement
        {
            Orientation = LineOrientation.Vertical,
            Thickness = thickness,
            Color = color ?? System.Drawing.Color.Black,
        });

    /// <summary>Places a vertical stack of items in the slot.</summary>
    public void Column(Action<ColumnDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var column = new ColumnElement();
        configure(new ColumnDescriptor(column));
        _assign(column);
    }

    /// <summary>Places items side by side in the slot.</summary>
    public void Row(Action<RowDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var row = new RowElement();
        configure(new RowDescriptor(row));
        _assign(row);
    }

    /// <summary>Places stacked layers in the slot.</summary>
    public void Layers(Action<LayersDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var layers = new LayersElement();
        configure(new LayersDescriptor(layers));
        _assign(layers);
    }

    /// <summary>Places a custom element in the slot.</summary>
    public void Element(IElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        _assign(element);
    }
}
