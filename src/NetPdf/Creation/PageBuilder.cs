using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using PdfSharp.Pdf;

namespace NetPdf.Creation;

/// <summary>Fluent builder for the content of a single PDF page.</summary>
public sealed class PageBuilder : IDisposable
{
    private readonly PdfPage _page;
    private XGraphics _gfx;

    internal PageBuilder(PdfPage page)
    {
        _page = page;
        _gfx = XGraphics.FromPdfPage(page);
    }

    /// <summary>Sets the page size (e.g. A4, Letter).</summary>
    public PageBuilder Size(PageSize size)
    {
        _gfx.Dispose();
        _page.Size = size;
        _gfx = XGraphics.FromPdfPage(_page);
        return this;
    }

    /// <summary>Sets landscape or portrait orientation.</summary>
    public PageBuilder Landscape(bool landscape = true)
    {
        _gfx.Dispose();
        _page.Orientation = landscape ? PageOrientation.Landscape : PageOrientation.Portrait;
        _gfx = XGraphics.FromPdfPage(_page);
        return this;
    }

    /// <summary>Draws text at the given position (points from the top-left corner).</summary>
    public PageBuilder AddText(string text, double x, double y, Action<TextOptions>? configure = null)
    {
        var opts = new TextOptions();
        configure?.Invoke(opts);

        var style = (opts.IsBold, opts.IsItalic) switch
        {
            (true, true) => XFontStyleEx.BoldItalic,
            (true, false) => XFontStyleEx.Bold,
            (false, true) => XFontStyleEx.Italic,
            _ => XFontStyleEx.Regular,
        };
        if (opts.IsUnderline)
            style |= XFontStyleEx.Underline;
        if (opts.IsStrikethrough)
            style |= XFontStyleEx.Strikeout;

        var font = new XFont(opts.FontFamily, opts.Size, style);
        var brush = MakeBrush(opts.Color);

        // Alignment implies wrapping; default the wrap width to the remaining page width.
        var width = opts.MaxWidth ?? (opts.Alignment is not null || opts.LineSpacingMultiplier != 1.0
            ? _page.Width.Point - x
            : (double?)null);

        if (width is { } w && opts.LineSpacingMultiplier != 1.0)
        {
            DrawSpacedText(text, font, brush, x, y, w, opts.Alignment ?? TextAlignment.Left,
                opts.LineSpacingMultiplier);
        }
        else if (width is { } w2)
        {
            var tf = new XTextFormatter(_gfx)
            {
                Alignment = (opts.Alignment ?? TextAlignment.Left) switch
                {
                    TextAlignment.Center => XParagraphAlignment.Center,
                    TextAlignment.Right => XParagraphAlignment.Right,
                    TextAlignment.Justify => XParagraphAlignment.Justify,
                    _ => XParagraphAlignment.Left,
                },
            };
            tf.DrawString(text, font, brush, new XRect(x, y, w2, _page.Height.Point - y));
        }
        else
        {
            _gfx.DrawString(text, font, brush, new XPoint(x, y + font.GetHeight()));
        }

        return this;
    }

    private void DrawSpacedText(string text, XFont font, XBrush brush, double x, double y,
        double maxWidth, TextAlignment alignment, double lineSpacing)
    {
        var lineHeight = font.GetHeight() * lineSpacing;
        var lineY = y;
        foreach (var paragraph in text.Split('\n'))
        {
            var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var line = "";
            foreach (var word in words)
            {
                var candidate = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && _gfx.MeasureString(candidate, font).Width > maxWidth)
                {
                    DrawAlignedLine(line, font, brush, x, lineY, maxWidth, alignment);
                    lineY += lineHeight;
                    line = word;
                }
                else
                {
                    line = candidate;
                }
            }
            DrawAlignedLine(line, font, brush, x, lineY, maxWidth, alignment);
            lineY += lineHeight;
        }
    }

    private void DrawAlignedLine(string line, XFont font, XBrush brush, double x, double y,
        double maxWidth, TextAlignment alignment)
    {
        if (line.Length == 0)
            return;
        var lineX = alignment switch
        {
            TextAlignment.Center => x + (maxWidth - _gfx.MeasureString(line, font).Width) / 2,
            TextAlignment.Right => x + maxWidth - _gfx.MeasureString(line, font).Width,
            _ => x, // Justify falls back to Left on the line-spacing path.
        };
        _gfx.DrawString(line, font, brush, new XPoint(lineX, y + font.GetHeight()));
    }

    /// <summary>Draws an image file (PNG/JPEG) at the given position. Width/height in points; omit one to keep the aspect ratio.</summary>
    public PageBuilder AddImage(string imagePath, double x, double y, double? width = null, double? height = null)
    {
        using var image = XImage.FromFile(imagePath);
        DrawImage(image, x, y, width, height);
        return this;
    }

    /// <summary>Draws an image from a stream at the given position. Width/height in points; omit one to keep the aspect ratio.</summary>
    public PageBuilder AddImage(Stream imageStream, double x, double y, double? width = null, double? height = null)
    {
        using var image = XImage.FromStream(imageStream);
        DrawImage(image, x, y, width, height);
        return this;
    }

    /// <summary>Draws a rectangle. Set <paramref name="fill"/> to fill it; otherwise only the outline is drawn.</summary>
    public PageBuilder AddRectangle(double x, double y, double width, double height,
        System.Drawing.Color? stroke = null, System.Drawing.Color? fill = null, double lineWidth = 1)
    {
        var rect = new XRect(x, y, width, height);
        if (fill is { } f)
            _gfx.DrawRectangle(MakeBrush(f), rect);
        _gfx.DrawRectangle(MakePen(stroke, lineWidth), rect);
        return this;
    }

    /// <summary>Draws a straight line between two points.</summary>
    public PageBuilder AddLine(double x1, double y1, double x2, double y2,
        System.Drawing.Color? color = null, double lineWidth = 1)
    {
        _gfx.DrawLine(MakePen(color, lineWidth), x1, y1, x2, y2);
        return this;
    }

    /// <summary>Draws an ellipse inside the given bounding box. Set <paramref name="fill"/> to fill it; otherwise only the outline is drawn.</summary>
    public PageBuilder AddEllipse(double x, double y, double width, double height,
        System.Drawing.Color? stroke = null, System.Drawing.Color? fill = null, double lineWidth = 1)
    {
        var rect = new XRect(x, y, width, height);
        if (fill is { } f)
            _gfx.DrawEllipse(MakeBrush(f), rect);
        _gfx.DrawEllipse(MakePen(stroke, lineWidth), rect);
        return this;
    }

    /// <summary>Draws a closed polygon through the given points. Set <paramref name="fill"/> to fill it; otherwise only the outline is drawn.</summary>
    public PageBuilder AddPolygon(IReadOnlyList<(double X, double Y)> points,
        System.Drawing.Color? stroke = null, System.Drawing.Color? fill = null, double lineWidth = 1)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count < 3)
            throw new ArgumentException("A polygon requires at least three points.", nameof(points));
        var xPoints = points.Select(p => new XPoint(p.X, p.Y)).ToArray();
        if (fill is { } f)
            _gfx.DrawPolygon(MakeBrush(f), xPoints, XFillMode.Winding);
        _gfx.DrawPolygon(MakePen(stroke, lineWidth), xPoints);
        return this;
    }

    /// <summary>Draws a cubic Bézier curve from (x1, y1) to (x2, y2) with two control points.</summary>
    public PageBuilder AddBezier(double x1, double y1, double cx1, double cy1,
        double cx2, double cy2, double x2, double y2,
        System.Drawing.Color? color = null, double lineWidth = 1)
    {
        _gfx.DrawBezier(MakePen(color, lineWidth), x1, y1, cx1, cy1, cx2, cy2, x2, y2);
        return this;
    }

    /// <summary>Draws a rectangle with rounded corners. Set <paramref name="fill"/> to fill it; otherwise only the outline is drawn.</summary>
    public PageBuilder AddRoundedRectangle(double x, double y, double width, double height,
        double cornerRadius, System.Drawing.Color? stroke = null,
        System.Drawing.Color? fill = null, double lineWidth = 1)
    {
        var rect = new XRect(x, y, width, height);
        var corner = new XSize(cornerRadius * 2, cornerRadius * 2);
        if (fill is { } f)
            _gfx.DrawRoundedRectangle(MakeBrush(f), rect, corner);
        _gfx.DrawRoundedRectangle(MakePen(stroke, lineWidth), rect, corner);
        return this;
    }

    private static XPen MakePen(System.Drawing.Color? color, double lineWidth)
    {
        var c = color ?? System.Drawing.Color.Black;
        return new XPen(XColor.FromArgb(c.R, c.G, c.B), lineWidth);
    }

    private static XSolidBrush MakeBrush(System.Drawing.Color color) =>
        new(XColor.FromArgb(color.R, color.G, color.B));

    private void DrawImage(XImage image, double x, double y, double? width, double? height)
    {
        var w = width ?? (height is { } h ? h * image.PixelWidth / image.PixelHeight : image.PointWidth);
        var hh = height ?? (width is { } ww ? ww * image.PixelHeight / image.PixelWidth : image.PointHeight);
        _gfx.DrawImage(image, x, y, w, hh);
    }

    void IDisposable.Dispose() => _gfx.Dispose();
}

/// <summary>Horizontal alignment for wrapped text drawn with <see cref="PageBuilder.AddText"/>.</summary>
public enum TextAlignment
{
    /// <summary>Align lines to the left edge.</summary>
    Left,
    /// <summary>Center each line.</summary>
    Center,
    /// <summary>Align lines to the right edge.</summary>
    Right,
    /// <summary>Stretch lines to fill the width (not supported together with <see cref="TextOptions.LineSpacing"/>; falls back to left).</summary>
    Justify,
}

/// <summary>Options for text drawn with <see cref="PageBuilder.AddText"/>.</summary>
public sealed class TextOptions
{
    internal string FontFamily { get; private set; } = "Arial";
    internal double Size { get; private set; } = 12;
    internal bool IsBold { get; private set; }
    internal bool IsItalic { get; private set; }
    internal bool IsUnderline { get; private set; }
    internal bool IsStrikethrough { get; private set; }
    internal System.Drawing.Color Color { get; private set; } = System.Drawing.Color.Black;
    internal double? MaxWidth { get; private set; }
    internal TextAlignment? Alignment { get; private set; }
    internal double LineSpacingMultiplier { get; private set; } = 1.0;

    /// <summary>Sets the font family (must be installed on the system).</summary>
    public TextOptions Font(string family) { FontFamily = family; return this; }

    /// <summary>Sets the font size in points.</summary>
    public TextOptions FontSize(double points) { Size = points; return this; }

    /// <summary>Uses a bold face.</summary>
    public TextOptions Bold() { IsBold = true; return this; }

    /// <summary>Uses an italic face.</summary>
    public TextOptions Italic() { IsItalic = true; return this; }

    /// <summary>Sets the text color.</summary>
    public TextOptions WithColor(System.Drawing.Color color) { Color = color; return this; }

    /// <summary>Wraps text within the given width in points.</summary>
    public TextOptions Wrap(double maxWidth) { MaxWidth = maxWidth; return this; }

    /// <summary>Underlines the text.</summary>
    public TextOptions Underline() { IsUnderline = true; return this; }

    /// <summary>Strikes through the text.</summary>
    public TextOptions Strikethrough() { IsStrikethrough = true; return this; }

    /// <summary>
    /// Sets the horizontal alignment. Implies wrapping: if no <see cref="Wrap"/> width is set,
    /// the text wraps at the right page edge.
    /// </summary>
    public TextOptions Align(TextAlignment alignment) { Alignment = alignment; return this; }

    /// <summary>
    /// Sets the line spacing as a multiple of the font height (1.0 = normal). Implies wrapping:
    /// if no <see cref="Wrap"/> width is set, the text wraps at the right page edge.
    /// </summary>
    public TextOptions LineSpacing(double multiplier)
    {
        if (multiplier <= 0)
            throw new ArgumentOutOfRangeException(nameof(multiplier), "Line spacing must be positive.");
        LineSpacingMultiplier = multiplier;
        return this;
    }
}
