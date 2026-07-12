namespace NetPdf.Reading;

/// <summary>Facts about one digital signature found in a PDF.</summary>
public sealed record PdfSignatureInfo
{
    /// <summary>The signing certificate's subject distinguished name, when readable.</summary>
    public string? SignerSubject { get; init; }

    /// <summary>The claimed signing time from the signature dictionary, when present.</summary>
    public DateTimeOffset? SigningTime { get; init; }

    /// <summary>The signature encoding, e.g. <c>adbe.pkcs7.detached</c>.</summary>
    public required string SubFilter { get; init; }

    /// <summary>
    /// True when the signed byte ranges extend to the end of the file — i.e. no content
    /// was appended after signing.
    /// </summary>
    public required bool CoversWholeDocument { get; init; }

    /// <summary>
    /// True when the cryptographic signature verifies against the document bytes
    /// (certificate trust chains are not evaluated).
    /// </summary>
    public required bool IsIntact { get; init; }
}
