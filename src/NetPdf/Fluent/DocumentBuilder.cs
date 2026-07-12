using NetPdf.Creation;
using NetPdf.Layout;

namespace NetPdf.Fluent;

/// <summary>
/// Renders a described document. Rendering is two-pass: a first pass counts the pages so
/// <c>{total}</c> page numbers resolve, then a second pass with fresh element trees produces
/// the output.
/// </summary>
public sealed class DocumentBuilder
{
    private readonly IReadOnlyList<PageDescriptor> _pages;
    private bool _pdfA;
    private bool _tagged;

    internal DocumentBuilder(IReadOnlyList<PageDescriptor> pages) => _pages = pages;

    /// <summary>
    /// Targets PDF/A-2b conformance: an sRGB output intent is embedded and the PDF/A
    /// identification XMP packet is appended on save.
    /// </summary>
    public DocumentBuilder AsPdfA()
    {
        _pdfA = true;
        return this;
    }

    /// <summary>
    /// Enables tagged-PDF output: content marked with semantic roles (<c>.Role(...)</c>,
    /// <c>.Heading(...)</c>, image alt text) produces a structure tree for accessibility.
    /// </summary>
    public DocumentBuilder WithTagging()
    {
        _tagged = true;
        return this;
    }

    /// <summary>Renders the document to a file.</summary>
    public void Save(string path) => Compose().Save(path);

    /// <summary>Renders the document to a stream. The stream is left open.</summary>
    public void Save(Stream stream) => Compose().Save(stream);

    /// <summary>Renders the document and returns it as a byte array.</summary>
    public byte[] ToBytes() => Compose().ToBytes();

    private PdfBuilder Compose()
    {
        var totalPages = Render(PdfFile.Create(), new PageContext());
        var builder = PdfFile.Create();
        if (_pdfA)
            builder.AsPdfA();
        if (_tagged)
            builder.WithTagging();
        Render(builder, new PageContext { TotalPages = totalPages });
        return builder;
    }

    private int Render(PdfBuilder builder, PageContext context)
    {
        var count = 0;
        foreach (var page in _pages)
            count += builder.AddPageLayout(page.ToLayout(), context);
        return count;
    }
}
