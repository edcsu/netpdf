namespace NetPdf.Layout;

/// <summary>
/// An image payload for layout elements, decoupled from the rendering backend. The canvas decodes
/// the bytes and reports the intrinsic size via <see cref="ICanvas.MeasureImage"/>.
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
