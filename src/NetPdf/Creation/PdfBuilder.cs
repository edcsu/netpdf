using NetPdf.Layout;
using PdfSharp.Pdf;
using SharpDocument = PdfSharp.Pdf.PdfDocument;

namespace NetPdf.Creation;

/// <summary>Fluent builder for creating a new PDF document.</summary>
public sealed class PdfBuilder
{
    private readonly SharpDocument _document = new();
    private bool _pdfA;
    private TaggingSession? _tagging;

    internal PdfBuilder()
    {
        SystemFontResolver.Register();
    }

    /// <summary>
    /// Targets PDF/A-2b conformance: an sRGB output intent is embedded, the version is
    /// set to PDF 1.7, and the PDF/A identification XMP packet is appended on save.
    /// </summary>
    public PdfBuilder AsPdfA()
    {
        _pdfA = true;
        return this;
    }

    /// <summary>
    /// Enables tagged-PDF output: layout elements marked with semantic roles (see
    /// <see cref="Layout.Elements.SemanticElement"/>) emit marked content, and a
    /// structure tree is built on save.
    /// </summary>
    public PdfBuilder WithTagging()
    {
        _tagging ??= new TaggingSession();
        return this;
    }

    /// <summary>Sets document metadata (title, author, etc.).</summary>
    public PdfBuilder WithMetadata(Action<MetadataBuilder> configure)
    {
        configure(new MetadataBuilder(_document.Info));
        return this;
    }

    /// <summary>Adds a page and configures its content.</summary>
    public PdfBuilder AddPage(Action<PageBuilder> configure)
    {
        using var page = new PageBuilder(_document.AddPage());
        configure(page);
        return this;
    }

    /// <summary>
    /// Renders a layout element tree onto A4 pages with a 50-point margin, adding pages
    /// automatically until the content is fully rendered.
    /// </summary>
    public PdfBuilder AddContent(IElement content) =>
        AddContent(content, pageWidth: 595, pageHeight: 842, margin: 50);

    /// <summary>
    /// Renders a layout element tree onto pages of the given size (points), adding pages
    /// automatically until the content is fully rendered.
    /// </summary>
    public PdfBuilder AddContent(IElement content, double pageWidth, double pageHeight, double margin)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageHeight);
        ArgumentOutOfRangeException.ThrowIfNegative(margin);
        LayoutRenderer.Render(_document, content, pageWidth, pageHeight, margin, _tagging);
        return this;
    }

    /// <summary>Renders a page layout (header/content/footer slots) into the document.</summary>
    internal int AddPageLayout(PageLayout layout, PageContext context) =>
        LayoutRenderer.Render(_document, layout, context, _tagging);

    /// <summary>
    /// Configures the document outline (bookmarks). Call after the pages the bookmarks
    /// point to have been added.
    /// </summary>
    public PdfBuilder WithOutline(Action<OutlineBuilder> configure)
    {
        configure(new OutlineBuilder(_document, _document.Outlines));
        return this;
    }

    /// <summary>Writes the document to a file.</summary>
    public void Save(string path) => File.WriteAllBytes(path, ToBytes());

    /// <summary>Writes the document to a stream. The stream is left open.</summary>
    public void Save(Stream stream)
    {
        var bytes = ToBytes();
        stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>Returns the document as a byte array.</summary>
    public byte[] ToBytes()
    {
        EnsureAtLeastOnePage();
        if (_tagging is { } tagging)
            StructTreeBuilder.Apply(_document, tagging);
        if (_pdfA)
            PdfAConformance.Apply(_document);
        using var ms = new MemoryStream();
        _document.Save(ms, false);
        var bytes = ms.ToArray();
        if (_pdfA)
        {
            // The identification packet must go in after the save, because PDFsharp
            // regenerates /Metadata from the Info dictionary on every save.
            var packet = Manipulation.XmpWriter.GeneratePacket(
                new Reading.PdfMetadata
                {
                    Title = _document.Info.Title is { Length: > 0 } t ? t : null,
                    Author = _document.Info.Author is { Length: > 0 } a ? a : null,
                },
                pdfAIdentification: true);
            bytes = Manipulation.XmpWriter.SetXmp(bytes, packet);
        }
        return bytes;
    }

    private void EnsureAtLeastOnePage()
    {
        if (_document.PageCount == 0)
            _document.AddPage();
    }
}

/// <summary>Fluent builder for PDF document metadata.</summary>
public sealed class MetadataBuilder
{
    private readonly PdfDocumentInformation _info;

    internal MetadataBuilder(PdfDocumentInformation info) => _info = info;

    /// <summary>Sets the document title.</summary>
    public MetadataBuilder Title(string title) { _info.Title = title; return this; }

    /// <summary>Sets the document author.</summary>
    public MetadataBuilder Author(string author) { _info.Author = author; return this; }

    /// <summary>Sets the document subject.</summary>
    public MetadataBuilder Subject(string subject) { _info.Subject = subject; return this; }

    /// <summary>Sets the document keywords.</summary>
    public MetadataBuilder Keywords(string keywords) { _info.Keywords = keywords; return this; }
}
