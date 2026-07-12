using PdfSharp.Pdf;

namespace NetPdf.Layout;

/// <summary>Semantic roles for tagged-PDF structure elements (accessibility).</summary>
public enum SemanticRole
{
    /// <summary>The document root.</summary>
    Document,
    /// <summary>A level-1 heading.</summary>
    Heading1,
    /// <summary>A level-2 heading.</summary>
    Heading2,
    /// <summary>A level-3 heading.</summary>
    Heading3,
    /// <summary>A level-4 heading.</summary>
    Heading4,
    /// <summary>A level-5 heading.</summary>
    Heading5,
    /// <summary>A level-6 heading.</summary>
    Heading6,
    /// <summary>A paragraph of text.</summary>
    Paragraph,
    /// <summary>A table.</summary>
    Table,
    /// <summary>A table row.</summary>
    TableRow,
    /// <summary>A table header cell.</summary>
    TableHeaderCell,
    /// <summary>A table data cell.</summary>
    TableCell,
    /// <summary>An illustration; give it alternate text.</summary>
    Figure,
    /// <summary>A list.</summary>
    List,
    /// <summary>A list item.</summary>
    ListItem,
}

/// <summary>
/// Optional canvas capability for emitting tagged-PDF marked content. Canvases that do
/// not support tagging simply don't implement this interface; elements no-op then.
/// </summary>
public interface ITagCanvas
{
    /// <summary>
    /// Opens a marked-content sequence with the given role, returning its MCID
    /// (or -1 when tagging is inactive). Balance with <see cref="EndMarkedContent"/>.
    /// </summary>
    int BeginMarkedContent(SemanticRole role, string? altText);

    /// <summary>Closes the most recently opened marked-content sequence.</summary>
    void EndMarkedContent();
}

/// <summary>One tagged content span recorded during rendering.</summary>
internal sealed class TagEntry
{
    public required int Mcid { get; init; }
    public required SemanticRole Role { get; init; }
    public string? AltText { get; init; }
    public required PdfPage Page { get; init; }
    public TagEntry? Parent { get; init; }
    public List<TagEntry> Children { get; } = [];
}

/// <summary>
/// Collects the marked-content spans emitted while rendering a tagged document, preserving
/// their nesting, so the structure tree can be built after all pages are drawn.
/// </summary>
internal sealed class TaggingSession
{
    private readonly Dictionary<PdfPage, int> _mcidCounters = [];
    private TagEntry? _current;

    /// <summary>Top-level tagged spans in reading order.</summary>
    internal List<TagEntry> Roots { get; } = [];

    /// <summary>Pages that contain tagged content, in first-use order.</summary>
    internal List<PdfPage> Pages { get; } = [];

    /// <summary>Opens a span on the given page and returns its page-local MCID.</summary>
    internal int Begin(PdfPage page, SemanticRole role, string? altText)
    {
        if (!_mcidCounters.TryGetValue(page, out var next))
            Pages.Add(page);
        _mcidCounters[page] = next + 1;

        var entry = new TagEntry
        {
            Mcid = next,
            Role = role,
            AltText = altText,
            Page = page,
            Parent = _current,
        };
        if (_current is null)
            Roots.Add(entry);
        else
            _current.Children.Add(entry);
        _current = entry;
        return next;
    }

    /// <summary>Closes the innermost open span.</summary>
    internal void End() => _current = _current?.Parent;

    /// <summary>Maps a role to its standard structure type name (without the leading slash).</summary>
    internal static string StructureType(SemanticRole role) => role switch
    {
        SemanticRole.Document => "Document",
        SemanticRole.Heading1 => "H1",
        SemanticRole.Heading2 => "H2",
        SemanticRole.Heading3 => "H3",
        SemanticRole.Heading4 => "H4",
        SemanticRole.Heading5 => "H5",
        SemanticRole.Heading6 => "H6",
        SemanticRole.Paragraph => "P",
        SemanticRole.Table => "Table",
        SemanticRole.TableRow => "TR",
        SemanticRole.TableHeaderCell => "TH",
        SemanticRole.TableCell => "TD",
        SemanticRole.Figure => "Figure",
        SemanticRole.List => "L",
        SemanticRole.ListItem => "LI",
        _ => "P",
    };
}
