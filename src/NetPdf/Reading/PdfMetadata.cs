namespace NetPdf.Reading;

/// <summary>Document information read from a PDF file.</summary>
public sealed record PdfMetadata
{
    /// <summary>The document title, if set.</summary>
    public string? Title { get; init; }

    /// <summary>The document author, if set.</summary>
    public string? Author { get; init; }

    /// <summary>The document subject, if set.</summary>
    public string? Subject { get; init; }

    /// <summary>The document keywords, if set.</summary>
    public string? Keywords { get; init; }

    /// <summary>The application that created the document, if recorded.</summary>
    public string? Creator { get; init; }

    /// <summary>The application that produced the PDF, if recorded.</summary>
    public string? Producer { get; init; }
}
