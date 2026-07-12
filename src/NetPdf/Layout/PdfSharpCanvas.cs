using NetPdf.Creation;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace NetPdf.Layout;

/// <summary>PDFsharp-backed canvas drawing onto one page's <see cref="XGraphics"/>.</summary>
internal sealed class PdfSharpCanvas : ICanvas, ITagCanvas
{
    private readonly XGraphics _gfx;
    private readonly PdfPage? _page;
    private readonly TaggingSession? _tagging;
    private readonly Stack<XGraphicsState> _states = new();
    private readonly Stack<TextStyle> _defaultStyles = new();
    private readonly Dictionary<TextStyle, XFont> _fonts = [];
    private readonly Dictionary<ImageSource, XImage> _images = [];

    internal PdfSharpCanvas(XGraphics gfx, PageContext? pageContext = null, PdfPage? page = null,
        TaggingSession? tagging = null)
    {
        SystemFontResolver.Register();
        _gfx = gfx;
        _page = page;
        _tagging = tagging;
        PageContext = pageContext ?? new PageContext();
        _defaultStyles.Push(new TextStyle());
    }

    /// <inheritdoc />
    public int BeginMarkedContent(SemanticRole role, string? altText)
    {
        if (_tagging is null || _page is null)
            return -1;
        var mcid = _tagging.Begin(_page, role, altText);
        // Sentinel comment; replaced by a real BDC operator in a post-pass (XGraphics has
        // no marked-content API).
        _gfx.WriteComment($"MCB {mcid} {TaggingSession.StructureType(role)}");
        return mcid;
    }

    /// <inheritdoc />
    public void EndMarkedContent()
    {
        if (_tagging is null || _page is null)
            return;
        _gfx.WriteComment("MCE");
        _tagging.End();
    }

    public PageContext PageContext { get; }

    /// <summary>Number of distinct images decoded so far; exposed for shared-image tests.</summary>
    internal int CachedImageCount => _images.Count;

    public TextStyle DefaultTextStyle => _defaultStyles.Peek();

    public void PushDefaultTextStyle(TextStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);
        _defaultStyles.Push(style.Merge(DefaultTextStyle));
    }

    public void PopDefaultTextStyle()
    {
        if (_defaultStyles.Count <= 1)
            throw new InvalidOperationException("No default text style to pop.");
        _defaultStyles.Pop();
    }

    public void DrawLink(double x, double y, double width, double height, string url)
    {
        ArgumentException.ThrowIfNullOrEmpty(url);
        // The annotation needs page coordinates; ignore links when no page is attached
        // (e.g. measuring canvases).
        _page?.AddWebLink(
            new PdfRectangle(_gfx.Transformer.WorldToDefaultPage(new XRect(x, y, width, height))), url);
    }

    public void DrawText(string text, TextStyle style, double x, double y)
    {
        var resolved = style.Resolve();
        var font = GetFont(resolved);
        var color = resolved.Color!.Value;
        var brush = new XSolidBrush(XColor.FromArgb(color.R, color.G, color.B));
        // DrawString positions text on the baseline; shift down so (x, y) is the top-left corner.
        var baseline = y + font.GetHeight();
        var spacing = resolved.LetterSpacing!.Value;
        if (spacing == 0)
        {
            _gfx.DrawString(text, font, brush, new XPoint(x, baseline));
            return;
        }

        // Letter spacing: draw per character, advancing by the character width plus the spacing.
        var cursor = x;
        foreach (var ch in text)
        {
            var s = ch.ToString();
            _gfx.DrawString(s, font, brush, new XPoint(cursor, baseline));
            cursor += _gfx.MeasureString(s, font).Width + spacing;
        }
    }

    public Size MeasureText(string text, TextStyle style)
    {
        var resolved = style.Resolve();
        var font = GetFont(resolved);
        if (text.Length == 0)
            return new Size(0, font.GetHeight());

        var spacing = resolved.LetterSpacing!.Value;
        var width = spacing == 0
            ? _gfx.MeasureString(text, font).Width
            : text.Sum(ch => _gfx.MeasureString(ch.ToString(), font).Width + spacing) - spacing;
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

    public void Rotate(double degrees) => _gfx.RotateTransform(degrees);

    public void Scale(double scaleX, double scaleY) => _gfx.ScaleTransform(scaleX, scaleY);

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

    // Expects a resolved style (no null properties).
    private XFont GetFont(TextStyle style)
    {
        if (_fonts.TryGetValue(style, out var font))
            return font;

        var face = XFontStyleEx.Regular;
        if (style.Bold!.Value)
            face |= XFontStyleEx.Bold;
        if (style.Italic!.Value)
            face |= XFontStyleEx.Italic;
        if (style.Underline!.Value)
            face |= XFontStyleEx.Underline;
        font = new XFont(style.FontFamily!, style.FontSize!.Value, face);
        _fonts[style] = font;
        return font;
    }
}
