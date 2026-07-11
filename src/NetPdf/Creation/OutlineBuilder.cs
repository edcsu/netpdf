using PdfSharp.Pdf;
using SharpDocument = PdfSharp.Pdf.PdfDocument;

namespace NetPdf.Creation;

/// <summary>Fluent builder for the document outline (bookmarks panel).</summary>
public sealed class OutlineBuilder
{
    private readonly SharpDocument _document;
    private readonly PdfOutlineCollection _outlines;

    internal OutlineBuilder(SharpDocument document, PdfOutlineCollection outlines)
    {
        _document = document;
        _outlines = outlines;
    }

    /// <summary>
    /// Adds a bookmark pointing to a page (0-based index), optionally with nested child bookmarks.
    /// The target page must already exist, so add bookmarks after the pages.
    /// </summary>
    public OutlineBuilder AddBookmark(string title, int pageIndex, Action<OutlineBuilder>? children = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(title);
        if (pageIndex < 0 || pageIndex >= _document.PageCount)
            throw new ArgumentOutOfRangeException(nameof(pageIndex),
                $"Page index {pageIndex} is out of range; the document has {_document.PageCount} page(s).");

        var outline = _outlines.Add(title, _document.Pages[pageIndex], opened: true);
        children?.Invoke(new OutlineBuilder(_document, outline.Outlines));
        return this;
    }
}
