using System.Runtime.Versioning;
using NetPdf.Creation;
using NetPdf.Manipulation;
using NetPdf.Reading;
using NetPdf.Rendering;

namespace NetPdf;

/// <summary>Rotation amounts for <see cref="PdfDocument.RotatePage"/>.</summary>
public enum Rotation
{
    /// <summary>Rotate 90° clockwise.</summary>
    Clockwise90 = 90,
    /// <summary>Rotate 180°.</summary>
    Rotate180 = 180,
    /// <summary>Rotate 90° counter-clockwise.</summary>
    CounterClockwise90 = 270,
}

/// <summary>Encryption algorithms for <see cref="PdfDocument.Protect"/>.</summary>
public enum EncryptionAlgorithm
{
    /// <summary>AES 256-bit (PDF 2.0). Recommended.</summary>
    Aes256,
    /// <summary>RC4 128-bit. Weak by modern standards; use only when a legacy reader requires it.</summary>
    Rc4_128,
}

/// <summary>Entry point for creating and opening PDF documents.</summary>
public static class PdfFile
{
    /// <summary>Starts building a new PDF document.</summary>
    public static PdfBuilder Create() => new();

    /// <summary>Opens a PDF file from disk.</summary>
    public static PdfDocument Open(string path, string? password = null) =>
        new(File.ReadAllBytes(path), password);

    /// <summary>Opens a PDF from a stream. The stream is fully read and can be disposed afterwards.</summary>
    public static PdfDocument Open(Stream stream, string? password = null)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return new PdfDocument(ms.ToArray(), password);
    }

    /// <summary>Opens a PDF from a byte array.</summary>
    public static PdfDocument Open(byte[] bytes, string? password = null) => new(bytes, password);

    /// <summary>Merges multiple PDF files into one output file.</summary>
    public static void Merge(IEnumerable<string> inputPaths, string outputPath) =>
        File.WriteAllBytes(outputPath, PdfManipulator.Merge(inputPaths.Select(File.ReadAllBytes)));

    /// <summary>Merges multiple opened documents into a new document.</summary>
    public static PdfDocument Merge(params PdfDocument[] documents) =>
        new(PdfManipulator.Merge(documents.Select(d => d.ToBytes())));
}

/// <summary>
/// An opened PDF document. Reading, manipulation, and rendering all operate on the same
/// in-memory bytes; manipulation methods return a new <see cref="PdfDocument"/> and leave
/// the original unchanged.
/// </summary>
public sealed class PdfDocument : IDisposable
{
    private readonly byte[] _bytes;
    private readonly string? _password;
    private readonly Lazy<PdfReader> _reader;

    internal PdfDocument(byte[] bytes, string? password = null)
    {
        _bytes = bytes;
        _password = password;
        _reader = new Lazy<PdfReader>(() => new PdfReader(_bytes, _password));
    }

    /// <summary>The number of pages in the document.</summary>
    public int PageCount => _reader.Value.PageCount;

    /// <summary>The document metadata (title, author, …).</summary>
    public PdfMetadata Metadata => _reader.Value.Metadata;

    // ---- Reading ----

    /// <summary>Extracts the text of all pages.</summary>
    public string ExtractText() => _reader.Value.ExtractText();

    /// <summary>Extracts the text of a single page (0-based index).</summary>
    public string ExtractText(int pageIndex) => _reader.Value.ExtractText(pageIndex);

    /// <summary>Extracts the images embedded in a page (0-based index) as PNG bytes where possible.</summary>
    public IReadOnlyList<byte[]> GetImages(int pageIndex) => _reader.Value.GetImages(pageIndex);

    // ---- Manipulation (each returns a new document) ----

    /// <summary>Returns a new document containing only the given pages (0-based), in the given order.</summary>
    public PdfDocument ExtractPages(params int[] pageIndexes) =>
        new(PdfManipulator.ExtractPages(_bytes, pageIndexes, _password));

    /// <summary>Returns a new document with the given pages (0-based) removed.</summary>
    public PdfDocument DeletePages(params int[] pageIndexes) =>
        new(PdfManipulator.DeletePages(_bytes, new HashSet<int>(pageIndexes), _password));

    /// <summary>Returns a new document with the pages rearranged into the given 0-based order.</summary>
    public PdfDocument ReorderPages(params int[] newOrder) =>
        new(PdfManipulator.ExtractPages(_bytes, newOrder, _password));

    /// <summary>Returns a new document with one page rotated.</summary>
    public PdfDocument RotatePage(int pageIndex, Rotation rotation) =>
        new(PdfManipulator.RotatePage(_bytes, pageIndex, (int)rotation, _password), _password);

    /// <summary>Splits the document into files of <paramref name="pagesPerFile"/> pages each, written to <paramref name="outputDirectory"/>. Returns the paths written.</summary>
    public IReadOnlyList<string> Split(int pagesPerFile, string outputDirectory, string baseName = "part")
    {
        Directory.CreateDirectory(outputDirectory);
        var parts = PdfManipulator.Split(_bytes, pagesPerFile, _password);
        var paths = new List<string>(parts.Count);
        for (var i = 0; i < parts.Count; i++)
        {
            var path = Path.Combine(outputDirectory, $"{baseName}-{i + 1}.pdf");
            File.WriteAllBytes(path, parts[i]);
            paths.Add(path);
        }
        return paths;
    }

    /// <summary>Returns a new document with updated metadata.</summary>
    public PdfDocument WithMetadata(Action<MetadataBuilder> configure) =>
        new(PdfManipulator.SetMetadata(_bytes, info => configure(new MetadataBuilder(info)), _password), _password);

    /// <summary>
    /// Returns a new document with a page of <paramref name="stamp"/> drawn on top of the
    /// selected pages (0-based; none selected = all pages). The stamp page is scaled to each
    /// target page. Useful for watermarks and "approved" stamps.
    /// </summary>
    public PdfDocument Overlay(PdfDocument stamp, int stampPageIndex = 0, params int[] pageIndexes) =>
        new(PdfManipulator.Stamp(_bytes, stamp._bytes, stampPageIndex,
            new HashSet<int>(pageIndexes), under: false, _password), _password);

    /// <summary>
    /// Returns a new document with a page of <paramref name="stamp"/> drawn beneath the content
    /// of the selected pages (0-based; none selected = all pages). The stamp page is scaled to
    /// each target page. Useful for letterheads and backgrounds.
    /// </summary>
    public PdfDocument Underlay(PdfDocument stamp, int stampPageIndex = 0, params int[] pageIndexes) =>
        new(PdfManipulator.Stamp(_bytes, stamp._bytes, stampPageIndex,
            new HashSet<int>(pageIndexes), under: true, _password), _password);

    /// <summary>Returns a new password-protected document (AES-256 by default).</summary>
    public PdfDocument Protect(string? userPassword = null, string? ownerPassword = null,
        EncryptionAlgorithm algorithm = EncryptionAlgorithm.Aes256) =>
        new(PdfManipulator.Protect(_bytes, userPassword, ownerPassword, algorithm, _password),
            userPassword ?? ownerPassword);

    /// <summary>
    /// Returns a new document with encryption removed. The document must have been opened
    /// with its password if one is required.
    /// </summary>
    public PdfDocument Decrypt() => new(PdfManipulator.Decrypt(_bytes, _password));

    /// <summary>Returns a new document with a file embedded under the given name.</summary>
    public PdfDocument AttachFile(string name, byte[] content)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(content);
        return new(PdfManipulator.AttachFile(_bytes, name, content, _password), _password);
    }

    /// <summary>Returns a new document with the file at <paramref name="path"/> embedded, named after its file name.</summary>
    public PdfDocument AttachFile(string path) =>
        AttachFile(Path.GetFileName(path), File.ReadAllBytes(path));

    /// <summary>The files embedded in the document.</summary>
    public IReadOnlyList<PdfAttachment> GetAttachments() => _reader.Value.GetAttachments();

    // ---- Rendering ----

    /// <summary>Renders a page (0-based index) to a PNG image.</summary>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst13.5")]
    [SupportedOSPlatform("android31.0")]
    [SupportedOSPlatform("ios13.6")]
    public byte[] RenderPage(int pageIndex, int dpi = 150) =>
        PdfRenderer.RenderPage(_bytes, pageIndex, dpi, _password);

    /// <summary>Renders every page to a PNG image.</summary>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst13.5")]
    [SupportedOSPlatform("android31.0")]
    [SupportedOSPlatform("ios13.6")]
    public IReadOnlyList<byte[]> RenderAllPages(int dpi = 150)
    {
        var count = PdfRenderer.GetPageCount(_bytes, _password);
        var pages = new List<byte[]>(count);
        for (var i = 0; i < count; i++)
            pages.Add(RenderPage(i, dpi));
        return pages;
    }

    // ---- Output ----

    /// <summary>Writes the document to a file.</summary>
    public void Save(string path) => File.WriteAllBytes(path, _bytes);

    /// <summary>Writes the document to a stream. The stream is left open.</summary>
    public void Save(Stream stream) => stream.Write(_bytes, 0, _bytes.Length);

    /// <summary>Returns the raw PDF bytes.</summary>
    public byte[] ToBytes() => (byte[])_bytes.Clone();

    /// <inheritdoc />
    public void Dispose()
    {
        if (_reader.IsValueCreated)
            _reader.Value.Dispose();
    }
}
