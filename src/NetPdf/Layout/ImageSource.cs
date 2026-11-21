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
}
