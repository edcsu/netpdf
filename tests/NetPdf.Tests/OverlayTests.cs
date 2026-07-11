using Xunit;

namespace NetPdf.Tests;

public class OverlayTests
{
    private static byte[] CreateDoc(string text, int pages = 1)
    {
        var builder = PdfFile.Create();
        for (var i = 0; i < pages; i++)
            builder.AddPage(p => p.AddText(text, 50, 50));
        return builder.ToBytes();
    }

    [Fact]
    public void Overlay_stamps_every_page_by_default()
    {
        using var baseDoc = PdfFile.Open(CreateDoc("BASE", pages: 2));
        using var stamp = PdfFile.Open(CreateDoc("WATERMARK"));

        using var result = baseDoc.Overlay(stamp);

        Assert.Equal(2, result.PageCount);
        for (var i = 0; i < 2; i++)
        {
            var text = result.ExtractText(i);
            Assert.Contains("BASE", text);
            Assert.Contains("WATERMARK", text);
        }
    }

    [Fact]
    public void Underlay_adds_background_content()
    {
        using var baseDoc = PdfFile.Open(CreateDoc("CONTENT"));
        using var letterhead = PdfFile.Open(CreateDoc("LETTERHEAD"));

        using var result = baseDoc.Underlay(letterhead);

        var text = result.ExtractText(0);
        Assert.Contains("CONTENT", text);
        Assert.Contains("LETTERHEAD", text);
    }

    [Fact]
    public void Overlay_on_selected_pages_leaves_others_untouched()
    {
        using var baseDoc = PdfFile.Open(CreateDoc("BASE", pages: 3));
        using var stamp = PdfFile.Open(CreateDoc("STAMP"));

        using var result = baseDoc.Overlay(stamp, 0, 1);

        Assert.DoesNotContain("STAMP", result.ExtractText(0));
        Assert.Contains("STAMP", result.ExtractText(1));
        Assert.DoesNotContain("STAMP", result.ExtractText(2));
    }

    [Fact]
    public void Overlay_leaves_original_unchanged()
    {
        using var baseDoc = PdfFile.Open(CreateDoc("BASE"));
        using var stamp = PdfFile.Open(CreateDoc("STAMP"));

        using var _ = baseDoc.Overlay(stamp);

        Assert.DoesNotContain("STAMP", baseDoc.ExtractText());
    }

    [Fact]
    public void Invalid_stamp_page_index_throws()
    {
        using var baseDoc = PdfFile.Open(CreateDoc("BASE"));
        using var stamp = PdfFile.Open(CreateDoc("STAMP"));

        Assert.Throws<ArgumentOutOfRangeException>(() => baseDoc.Overlay(stamp, stampPageIndex: 5));
    }
}
