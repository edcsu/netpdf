using Xunit;

namespace NetPdf.Tests;

public class PdfFileTests
{
    private static byte[] CreateSample(string text = "Hello NetPdf", int pages = 1)
    {
        var builder = PdfFile.Create()
            .WithMetadata(m => m.Title("Test Doc").Author("NetPdf Tests"));
        for (var i = 0; i < pages; i++)
        {
            var pageText = pages == 1 ? text : $"{text} page {i + 1}";
            builder.AddPage(p => p
                .AddText(pageText, 50, 50, o => o.FontSize(18).Bold())
                .AddLine(50, 90, 300, 90)
                .AddRectangle(50, 100, 100, 40, fill: System.Drawing.Color.LightGray));
        }
        return builder.ToBytes();
    }

    [Fact]
    public void Create_then_read_back_text_and_metadata()
    {
        using var doc = PdfFile.Open(CreateSample("Round trip works"));

        Assert.Equal(1, doc.PageCount);
        Assert.Contains("Round trip works", doc.ExtractText());
        Assert.Equal("Test Doc", doc.Metadata.Title);
        Assert.Equal("NetPdf Tests", doc.Metadata.Author);
    }

    [Fact]
    public void Merge_combines_page_counts()
    {
        using var a = PdfFile.Open(CreateSample(pages: 2));
        using var b = PdfFile.Open(CreateSample(pages: 3));

        using var merged = PdfFile.Merge(a, b);

        Assert.Equal(5, merged.PageCount);
    }

    [Fact]
    public void Split_writes_one_file_per_page()
    {
        using var doc = PdfFile.Open(CreateSample(pages: 3));
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            var paths = doc.Split(pagesPerFile: 1, dir);

            Assert.Equal(3, paths.Count);
            foreach (var path in paths)
            {
                using var part = PdfFile.Open(path);
                Assert.Equal(1, part.PageCount);
            }
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void ExtractPages_and_DeletePages()
    {
        using var doc = PdfFile.Open(CreateSample(pages: 4));

        using var extracted = doc.ExtractPages(0, 2);
        Assert.Equal(2, extracted.PageCount);
        Assert.Contains("page 1", extracted.ExtractText(0));
        Assert.Contains("page 3", extracted.ExtractText(1));

        using var trimmed = doc.DeletePages(1, 3);
        Assert.Equal(2, trimmed.PageCount);
        Assert.Contains("page 1", trimmed.ExtractText(0));
        Assert.Contains("page 3", trimmed.ExtractText(1));
    }

    [Fact]
    public void ReorderPages_changes_page_order()
    {
        using var doc = PdfFile.Open(CreateSample(pages: 2));

        using var reordered = doc.ReorderPages(1, 0);

        Assert.Contains("page 2", reordered.ExtractText(0));
        Assert.Contains("page 1", reordered.ExtractText(1));
    }

    [Fact]
    public void RotatePage_survives_round_trip()
    {
        using var doc = PdfFile.Open(CreateSample());

        using var rotated = doc.RotatePage(0, Rotation.Clockwise90);

        Assert.Equal(1, rotated.PageCount);
        Assert.Contains("Hello NetPdf", rotated.ExtractText());
    }

    [Fact]
    public void Protect_requires_password_to_open()
    {
        using var doc = PdfFile.Open(CreateSample("secret content"));
        using var locked = doc.Protect(userPassword: "pw123");

        var bytes = locked.ToBytes();

        Assert.ThrowsAny<Exception>(() =>
        {
            using var noPw = PdfFile.Open(bytes);
            _ = noPw.PageCount;
        });

        using var withPw = PdfFile.Open(bytes, password: "pw123");
        Assert.Equal(1, withPw.PageCount);
    }

    [Fact]
    public void WithMetadata_updates_existing_document()
    {
        using var doc = PdfFile.Open(CreateSample());

        using var updated = doc.WithMetadata(m => m.Title("New Title"));

        Assert.Equal("New Title", updated.Metadata.Title);
    }

    [Fact]
    public void RenderPage_produces_valid_png()
    {
        using var doc = PdfFile.Open(CreateSample());

        var png = doc.RenderPage(0, dpi: 96);

        Assert.True(png.Length > 8);
        Assert.Equal([0x89, 0x50, 0x4E, 0x47], png.Take(4));
    }

    [Fact]
    public void Save_and_open_via_stream()
    {
        using var doc = PdfFile.Open(CreateSample("stream test"));
        using var ms = new MemoryStream();
        doc.Save(ms);
        ms.Position = 0;

        using var reopened = PdfFile.Open(ms);

        Assert.Contains("stream test", reopened.ExtractText());
    }
}
