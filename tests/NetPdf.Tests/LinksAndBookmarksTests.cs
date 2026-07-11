using UglyToad.PdfPig.Annotations;
using PigDocument = UglyToad.PdfPig.PdfDocument;
using UglyToad.PdfPig.Outline;
using Xunit;

namespace NetPdf.Tests;

public class LinksAndBookmarksTests
{
    [Fact]
    public void Web_link_round_trips_as_link_annotation()
    {
        var bytes = PdfFile.Create()
            .AddPage(p => p
                .AddText("Visit example.com", 50, 50)
                .AddWebLink(50, 50, 150, 20, "https://example.com/"))
            .ToBytes();

        using var pig = PigDocument.Open(bytes);
        var annotations = pig.GetPage(1).ExperimentalAccess.GetAnnotations().ToList();
        var link = Assert.Single(annotations);
        Assert.Equal(AnnotationType.Link, link.Type);
        Assert.Contains("example.com", link.AnnotationDictionary.ToString());
    }

    [Fact]
    public void Document_link_round_trips_as_link_annotation()
    {
        var bytes = PdfFile.Create()
            .AddPage(p => p
                .AddText("Go to page 2", 50, 50)
                .AddDocumentLink(50, 50, 100, 20, destinationPageIndex: 1))
            .AddPage(p => p.AddText("Target page", 50, 50))
            .ToBytes();

        using var pig = PigDocument.Open(bytes);
        var link = Assert.Single(pig.GetPage(1).ExperimentalAccess.GetAnnotations().ToList());
        Assert.Equal(AnnotationType.Link, link.Type);
    }

    [Fact]
    public void Negative_document_link_index_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PdfFile.Create().AddPage(p => p.AddDocumentLink(0, 0, 10, 10, -1)).ToBytes());
    }

    [Fact]
    public void Nested_bookmarks_round_trip()
    {
        var bytes = PdfFile.Create()
            .AddPage(p => p.AddText("Chapter 1", 50, 50))
            .AddPage(p => p.AddText("Section 1.1", 50, 50))
            .AddPage(p => p.AddText("Chapter 2", 50, 50))
            .WithOutline(o => o
                .AddBookmark("Chapter 1", 0, c => c.AddBookmark("Section 1.1", 1))
                .AddBookmark("Chapter 2", 2))
            .ToBytes();

        using var pig = PigDocument.Open(bytes);
        Assert.True(pig.TryGetBookmarks(out var bookmarks));
        var roots = bookmarks.Roots;
        Assert.Equal(2, roots.Count);
        Assert.Equal("Chapter 1", roots[0].Title);
        Assert.Equal("Chapter 2", roots[1].Title);
        var child = Assert.Single(roots[0].Children);
        Assert.Equal("Section 1.1", child.Title);
    }

    [Fact]
    public void Bookmark_to_missing_page_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PdfFile.Create()
                .AddPage(p => p.AddText("only page", 50, 50))
                .WithOutline(o => o.AddBookmark("bad", 5))
                .ToBytes());
    }
}
