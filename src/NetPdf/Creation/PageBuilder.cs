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

        var font = new XFont(opts.FontFamily, opts.Size, style);
        var brush = new XSolidBrush(XColor.FromArgb(opts.Color.R, opts.Color.G, opts.Color.B));

        if (opts.MaxWidth is { } width)
        {
            var tf = new XTextFormatter(_gfx);
            tf.DrawString(text, font, brush, new XRect(x, y, width, _page.Height.Point - y));
        }
        else
        {
            _gfx.DrawString(text, font, brush, new XPoint(x, y + font.GetHeight()));
        }

        return this;
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
            _gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(f.R, f.G, f.B)), rect);
        var s = stroke ?? System.Drawing.Color.Black;
        _gfx.DrawRectangle(new XPen(XColor.FromArgb(s.R, s.G, s.B), lineWidth), rect);
        return this;
    }

    /// <summary>Draws a straight line between two points.</summary>
    public PageBuilder AddLine(double x1, double y1, double x2, double y2,
        System.Drawing.Color? color = null, double lineWidth = 1)
    {
        var c = color ?? System.Drawing.Color.Black;
        _gfx.DrawLine(new XPen(XColor.FromArgb(c.R, c.G, c.B), lineWidth), x1, y1, x2, y2);
        return this;
    }

    private void DrawImage(XImage image, double x, double y, double? width, double? height)
    {
        var w = width ?? (height is { } h ? h * image.PixelWidth / image.PixelHeight : image.PointWidth);
        var hh = height ?? (width is { } ww ? ww * image.PixelHeight / image.PixelWidth : image.PointHeight);
        _gfx.DrawImage(image, x, y, w, hh);
    }

    void IDisposable.Dispose() => _gfx.Dispose();
}

/// <summary>Options for text drawn with <see cref="PageBuilder.AddText"/>.</summary>
public sealed class TextOptions
{
    internal string FontFamily { get; private set; } = "Arial";
    internal double Size { get; private set; } = 12;
    internal bool IsBold { get; private set; }
    internal bool IsItalic { get; private set; }
    internal System.Drawing.Color Color { get; private set; } = System.Drawing.Color.Black;
    internal double? MaxWidth { get; private set; }

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
}
