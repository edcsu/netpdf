namespace NetPdf.Layout;

/// <summary>
/// Page numbering state exposed to elements through <see cref="ICanvas.PageContext"/>.
/// The render loop updates <see cref="CurrentPage"/> as pages are added; <see cref="TotalPages"/>
/// stays 0 until a first render pass has counted the document's pages.
/// </summary>
public sealed class PageContext
{
    /// <summary>The 1-based number of the page currently being rendered (0 before rendering starts).</summary>
    public int CurrentPage { get; internal set; }

    /// <summary>The total number of pages, or 0 while still unknown.</summary>
    public int TotalPages { get; internal set; }
}
