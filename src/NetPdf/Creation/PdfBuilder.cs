using PdfSharp.Pdf;
using SharpDocument = PdfSharp.Pdf.PdfDocument;

namespace NetPdf.Creation;

/// <summary>Fluent builder for creating a new PDF document.</summary>
public sealed class PdfBuilder
{
    private readonly SharpDocument _document = new();

    internal PdfBuilder()
    {
        SystemFontResolver.Register();
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

    /// <summary>Writes the document to a file.</summary>
    public void Save(string path)
    {
        EnsureAtLeastOnePage();
        _document.Save(path);
    }

    /// <summary>Writes the document to a stream. The stream is left open.</summary>
    public void Save(Stream stream)
    {
        EnsureAtLeastOnePage();
        _document.Save(stream, false);
    }

    /// <summary>Returns the document as a byte array.</summary>
    public byte[] ToBytes()
    {
        using var ms = new MemoryStream();
        Save(ms);
        return ms.ToArray();
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
