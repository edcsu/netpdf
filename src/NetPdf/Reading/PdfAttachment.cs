namespace NetPdf.Reading;

/// <summary>A file embedded in a PDF document.</summary>
public sealed record PdfAttachment
{
    /// <summary>The attachment's file name.</summary>
    public required string Name { get; init; }

    /// <summary>The attachment's content.</summary>
    public required byte[] Content { get; init; }
}
