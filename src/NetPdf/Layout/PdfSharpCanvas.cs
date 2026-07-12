using NetPdf.Creation;
using PdfSharp.Drawing;

namespace NetPdf.Layout;

/// <summary>PDFsharp-backed canvas drawing onto one page's <see cref="XGraphics"/>.</summary>
internal sealed class PdfSharpCanvas : ICanvas
{
    private readonly XGraphics _gfx;
    private readonly Stack<XGraphicsState> _states = new();
    private readonly Dictionary<TextStyle, XFont> _fonts = [];
    private readonly Dictionary<ImageSource, XImage> _images = [];

    internal PdfSharpCanvas(XGraphics gfx, PageContext? pageContext = null)
    {
        SystemFontResolver.Register();
        _gfx = gfx;
        PageContext = pageContext ?? new PageContext();
    }

    public PageContext PageContext { get; }

    public void DrawText(string text, TextStyle style, double x, double y)
    {
        var font = GetFont(style);
        var brush = new XSolidBrush(XColor.FromArgb(style.Color.R, style.Color.G, style.Color.B));
        // DrawString positions text on the baseline; shift down so (x, y) is the top-left corner.
        _gfx.DrawString(text, font, brush, new XPoint(x, y + font.GetHeight()));
    }

    public Size MeasureText(string text, TextStyle style)
    {
        var font = GetFont(style);
        var width = text.Length == 0 ? 0 : _gfx.MeasureString(text, font).Width;
        return new Size(width, font.GetHeight());
    }

    public Size MeasureImage(ImageSource image)
    {
        var img = GetImage(image);
        return new Size(img.PointWidth, img.PointHeight);
    }

    public void DrawImage(ImageSource image, double x, double y, double width, double height) =>
        _gfx.DrawImage(GetImage(image), new XRect(x, y, width, height));

    public void DrawLine(double x1, double y1, double x2, double y2, System.Drawing.Color color, double thickness) =>
        _gfx.DrawLine(MakePen(color, thickness), x1, y1, x2, y2);

    public void DrawRectangle(double x, double y, double width, double height,
        System.Drawing.Color? fill, System.Drawing.Color? stroke = null,
        double strokeThickness = 1, double cornerRadius = 0)
    {
        var rect = new XRect(x, y, width, height);
        if (cornerRadius > 0)
        {
            var corner = new XSize(cornerRadius * 2, cornerRadius * 2);
            if (fill is { } f)
                _gfx.DrawRoundedRectangle(MakeBrush(f), rect, corner);
            if (stroke is { } s)
                _gfx.DrawRoundedRectangle(MakePen(s, strokeThickness), rect, corner);
        }
        else
        {
            if (fill is { } f)
                _gfx.DrawRectangle(MakeBrush(f), rect);
            if (stroke is { } s)
                _gfx.DrawRectangle(MakePen(s, strokeThickness), rect);
        }
    }

    public void Translate(double dx, double dy) => _gfx.TranslateTransform(dx, dy);

    public void Save() => _states.Push(_gfx.Save());

    public void Restore() => _gfx.Restore(_states.Pop());

    // Cached XImages are drawn across pages; PDFsharp keeps them alive with the document,
    // so they must not be disposed per draw call.
    private XImage GetImage(ImageSource source)
    {
        if (_images.TryGetValue(source, out var img))
            return img;

        // PDFsharp calls MemoryStream.GetBuffer(), so the buffer must be publicly visible.
        img = XImage.FromStream(new MemoryStream(source.Data, 0, source.Data.Length,
            writable: false, publiclyVisible: true));
        _images[source] = img;
        return img;
    }

    private static XPen MakePen(System.Drawing.Color color, double thickness) =>
        new(XColor.FromArgb(color.R, color.G, color.B), thickness);

    private static XSolidBrush MakeBrush(System.Drawing.Color color) =>
        new(XColor.FromArgb(color.R, color.G, color.B));

    private XFont GetFont(TextStyle style)
    {
        if (_fonts.TryGetValue(style, out var font))
            return font;

        var face = (style.Bold, style.Italic) switch
        {
            (true, true) => XFontStyleEx.BoldItalic,
            (true, false) => XFontStyleEx.Bold,
            (false, true) => XFontStyleEx.Italic,
            _ => XFontStyleEx.Regular,
        };
        font = new XFont(style.FontFamily, style.FontSize, face);
        _fonts[style] = font;
        return font;
    }
}
