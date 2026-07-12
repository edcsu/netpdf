using NetPdf.Creation;
using PdfSharp.Drawing;

namespace NetPdf.Layout;

/// <summary>PDFsharp-backed canvas drawing onto one page's <see cref="XGraphics"/>.</summary>
internal sealed class PdfSharpCanvas : ICanvas
{
    private readonly XGraphics _gfx;
    private readonly Stack<XGraphicsState> _states = new();
    private readonly Dictionary<TextStyle, XFont> _fonts = [];

    internal PdfSharpCanvas(XGraphics gfx)
    {
        SystemFontResolver.Register();
        _gfx = gfx;
    }

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

    public void Translate(double dx, double dy) => _gfx.TranslateTransform(dx, dy);

    public void Save() => _states.Push(_gfx.Save());

    public void Restore() => _gfx.Restore(_states.Pop());

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
