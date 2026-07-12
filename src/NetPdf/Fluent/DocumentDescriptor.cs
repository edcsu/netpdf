namespace NetPdf.Fluent;

/// <summary>Configures a document as a sequence of page sections.</summary>
public sealed class DocumentDescriptor
{
    internal List<PageDescriptor> Pages { get; } = [];

    internal DocumentDescriptor()
    {
    }

    /// <summary>Adds a page section and configures it. Sections render one after another.</summary>
    public DocumentDescriptor Page(Action<PageDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var page = new PageDescriptor();
        configure(page);
        Pages.Add(page);
        return this;
    }
}
