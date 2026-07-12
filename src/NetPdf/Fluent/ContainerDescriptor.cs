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

    /// <summary>
    /// Rotates the content clockwise by the given angle around the slot's top-left corner.
    /// Visual only: the measured size is unchanged, so rotated content may overflow the slot.
    /// </summary>
    public ContainerDescriptor Rotate(double degrees) => Wrap(new RotateElement { Degrees = degrees });

    /// <summary>Scales the content uniformly; scaling affects the measured size.</summary>
    public ContainerDescriptor Scale(double factor) => Scale(factor, factor);

    /// <summary>Scales the content per axis; scaling affects the measured size. Negative factors mirror.</summary>
    public ContainerDescriptor Scale(double scaleX, double scaleY) =>
        Wrap(new ScaleElement { ScaleX = scaleX, ScaleY = scaleY });

    /// <summary>Mirrors the content horizontally within its slot.</summary>
    public ContainerDescriptor FlipHorizontal() => Scale(-1, 1);

    /// <summary>Mirrors the content vertically within its slot.</summary>
    public ContainerDescriptor FlipVertical() => Scale(1, -1);

    /// <summary>Mirrors the content both horizontally and vertically (180° turn).</summary>
    public ContainerDescriptor FlipOver() => Scale(-1, -1);

    /// <summary>Shrinks the content uniformly until it fits entirely in the offered space.</summary>
    public ContainerDescriptor ScaleToFit() => Wrap(new ScaleToFitElement());

    /// <summary>Paints a solid background behind the content.</summary>
    public ContainerDescriptor Background(System.Drawing.Color color, double cornerRadius = 0) =>
        Wrap(new BackgroundElement { Color = color, CornerRadius = cornerRadius });

    /// <summary>
    /// Paints an approximated drop shadow behind the content. PDF has no native blur; see
    /// <see cref="ShadowStyle"/> for how blur is approximated.
    /// </summary>
    public ContainerDescriptor Shadow(ShadowStyle? style = null) =>
        Wrap(new ShadowElement { Style = style ?? new ShadowStyle() });

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

    /// <summary>
    /// Marks the content with a semantic role for tagged-PDF output (requires
    /// <c>WithTagging()</c> on the document; a no-op otherwise).
    /// </summary>
    public ContainerDescriptor Role(SemanticRole role, string? altText = null) =>
        Wrap(new SemanticElement { Role = role, AltText = altText });

    /// <summary>Marks the content as a heading of the given level (1–6) for tagged-PDF output.</summary>
    public ContainerDescriptor Heading(int level)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(level, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(level, 6);
        return Role(SemanticRole.Heading1 + (level - 1));
    }

    /// <summary>Marks the content as a paragraph for tagged-PDF output.</summary>
    public ContainerDescriptor Paragraph() => Role(SemanticRole.Paragraph);

    /// <summary>
    /// Overlays a colored outline and label on the content's area for layout debugging.
    /// Inspect via <c>PdfDocument.RenderPage</c> to PNG. Consumes no layout space.
    /// </summary>
    public ContainerDescriptor Debug(string label = "", System.Drawing.Color? color = null) =>
        Wrap(new DebugAreaElement { Label = label, Color = color });

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

    /// <summary>
    /// Places an image in the slot, scaled to the available width. When
    /// <paramref name="altText"/> is given, the image is tagged as a Figure with that
    /// alternate text in tagged-PDF output.
    /// </summary>
    public void Image(ImageSource source, string? altText = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        IElement element = new ImageElement(source);
        if (altText is not null)
            element = new SemanticElement { Role = SemanticRole.Figure, AltText = altText, Child = element };
        _assign(element);
    }

    /// <summary>Places an image loaded from a file in the slot, scaled to the available width.</summary>
    public void Image(string filePath) => Image(ImageSource.FromFile(filePath));

    /// <summary>
    /// Places an SVG in the slot, rasterized to a PNG at <paramref name="scale"/>× its intrinsic
    /// size and scaled to the available width like an image. Output is raster, not vector.
    /// </summary>
    public void Svg(string markup, double scale = 2) => Image(ImageSource.FromSvg(markup, scale));

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

    /// <summary>Places a table with fixed column widths, cell spans and repeating header/footer rows.</summary>
    public void Table(Action<TableDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var descriptor = new TableDescriptor();
        configure(descriptor);
        _assign(descriptor.Build());
    }

    /// <summary>Places stacked layers in the slot.</summary>
    public void Layers(Action<LayersDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var layers = new LayersElement();
        configure(new LayersDescriptor(layers));
        _assign(layers);
    }

    /// <summary>
    /// Places content with before/after slots repeated above and below it on every page
    /// the content spans.
    /// </summary>
    public void Decoration(Action<DecorationDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var descriptor = new DecorationDescriptor();
        configure(descriptor);
        _assign(descriptor.Build());
    }

    /// <summary>Places items flowed horizontally with wrapping, like words in a paragraph.</summary>
    public void Inlined(Action<InlinedDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var inlined = new InlinedElement();
        configure(new InlinedDescriptor(inlined));
        _assign(inlined);
    }

    /// <summary>Places a bulleted or numbered list in the slot.</summary>
    public void List(Action<ListDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var descriptor = new ListDescriptor();
        configure(descriptor);
        _assign(descriptor.Build());
    }

    /// <summary>
    /// Hands the raw canvas to <paramref name="draw"/> for custom drawing (charts, diagrams, …).
    /// The slot takes the full offered space; combine with <see cref="Width(double)"/> and
    /// <see cref="Height(double)"/> to bound it.
    /// </summary>
    public void Canvas(Action<Layout.ICanvas, Layout.Size> draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        _assign(new CanvasElement(draw));
    }

    /// <summary>Places a QR code in the slot, scaled to the available width like an image.</summary>
    public void QrCode(string content, int pixelSize = 256) =>
        Image(Creation.BarcodeGenerator.GenerateQrCode(content, pixelSize));

    /// <summary>Places a barcode in the slot, scaled to the available width like an image.</summary>
    public void Barcode(string content, Creation.BarcodeFormat format,
        int pixelWidth = 256, int pixelHeight = 256) =>
        Image(Creation.BarcodeGenerator.Generate(content, format, pixelWidth, pixelHeight));

    /// <summary>Places a custom element in the slot.</summary>
    public void Element(IElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        _assign(element);
    }
}
