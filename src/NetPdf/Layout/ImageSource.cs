namespace NetPdf.Layout;

/// <summary>
/// An image payload for layout elements, decoupled from the rendering backend. The canvas decodes
/// the bytes and reports the intrinsic size via <see cref="ICanvas.MeasureImage"/>.
/// Images are cached per <see cref="ImageSource"/> instance: reusing one instance across many
/// draw calls (or pages) embeds the image data in the document only once, so create the instance
/// once and share it rather than re-reading the same file per draw.
/// </summary>
public sealed class ImageSource
{
    private ImageSource(byte[] data) => Data = data;

    internal byte[] Data { get; }

    /// <summary>Loads an image from a file on disk.</summary>
    public static ImageSource FromFile(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        return new ImageSource(File.ReadAllBytes(path));
    }

    /// <summary>Wraps raw image bytes (PNG, JPEG, …).</summary>
    public static ImageSource FromBytes(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return new ImageSource(data);
    }

    /// <summary>Reads an image from a stream.</summary>
    public static ImageSource FromStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return new ImageSource(ms.ToArray());
    }

    /// <summary>
    /// Rasterizes SVG markup to a PNG at <paramref name="scale"/> times the SVG's intrinsic
    /// size (default 2× for crisp print output). The result is a raster image, not vector
    /// artwork; raise the scale for higher fidelity. Reuse the returned instance to embed the
    /// bitmap only once.
    /// </summary>
    public static ImageSource FromSvg(string markup, double scale = 2)
    {
        ArgumentException.ThrowIfNullOrEmpty(markup);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(scale);

        using var svg = new Svg.Skia.SKSvg();
        try
        {
            if (svg.FromSvg(markup) is null || svg.Picture is null)
                throw new ArgumentException("The SVG markup could not be parsed.", nameof(markup));
        }
        catch (System.Xml.XmlException e)
        {
            throw new ArgumentException("The SVG markup could not be parsed.", nameof(markup), e);
        }

        var bounds = svg.Picture.CullRect;
        var width = (int)Math.Ceiling(bounds.Width * scale);
        var height = (int)Math.Ceiling(bounds.Height * scale);
        if (width <= 0 || height <= 0)
            throw new ArgumentException("The SVG has no drawable area.", nameof(markup));

        using var bitmap = new SkiaSharp.SKBitmap(width, height);
        using (var skCanvas = new SkiaSharp.SKCanvas(bitmap))
        {
            skCanvas.Clear(SkiaSharp.SKColors.Transparent);
            skCanvas.Scale((float)scale);
            skCanvas.DrawPicture(svg.Picture);
        }

        using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, quality: 100);
        return new ImageSource(data.ToArray());
    }

    /// <summary>Rasterizes an SVG file to a PNG; see <see cref="FromSvg"/> for the caveats.</summary>
    public static ImageSource FromSvgFile(string path, double scale = 2)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        return FromSvg(File.ReadAllText(path), scale);
    }
}
