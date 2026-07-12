namespace NetPdf.Fluent;

/// <summary>Entry point for the fluent layout document API.</summary>
public static class Document
{
    /// <summary>
    /// Describes a document as page sections with header/content/footer slots. Content flows
    /// and paginates automatically; call <see cref="DocumentBuilder.Save(string)"/> or
    /// <see cref="DocumentBuilder.ToBytes"/> on the result to render it.
    /// </summary>
    public static DocumentBuilder Create(Action<DocumentDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var descriptor = new DocumentDescriptor();
        configure(descriptor);
        return new DocumentBuilder(descriptor.Pages);
    }
}
