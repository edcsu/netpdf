using System.Text;
using Xunit;

namespace NetPdf.Tests;

public class AttachmentTests
{
    private static byte[] CreateSample() =>
        PdfFile.Create().AddPage(p => p.AddText("Host document", 50, 50)).ToBytes();

    [Fact]
    public void Attachment_round_trips_byte_for_byte()
    {
        var payload = Encoding.UTF8.GetBytes("hello,attachment\n1,2,3");
        using var doc = PdfFile.Open(CreateSample());

        using var withFile = doc.AttachFile("data.csv", payload);
        using var reopened = PdfFile.Open(withFile.ToBytes());

        var attachment = Assert.Single(reopened.GetAttachments());
        Assert.Equal("data.csv", attachment.Name);
        Assert.Equal(payload, attachment.Content);
    }

    [Fact]
    public void Multiple_attachments_are_all_preserved()
    {
        using var doc = PdfFile.Open(CreateSample());

        using var result = doc
            .AttachFile("a.txt", Encoding.UTF8.GetBytes("first"))
            .AttachFile("b.txt", Encoding.UTF8.GetBytes("second"));
        using var reopened = PdfFile.Open(result.ToBytes());

        var attachments = reopened.GetAttachments();
        Assert.Equal(2, attachments.Count);
        Assert.Contains(attachments, a => a.Name == "a.txt" && Encoding.UTF8.GetString(a.Content) == "first");
        Assert.Contains(attachments, a => a.Name == "b.txt" && Encoding.UTF8.GetString(a.Content) == "second");
    }

    [Fact]
    public void Document_without_attachments_returns_empty_list()
    {
        using var doc = PdfFile.Open(CreateSample());
        Assert.Empty(doc.GetAttachments());
    }

    [Fact]
    public void Attached_document_still_extracts_text()
    {
        using var doc = PdfFile.Open(CreateSample());
        using var result = doc.AttachFile("x.bin", [1, 2, 3]);
        using var reopened = PdfFile.Open(result.ToBytes());

        Assert.Contains("Host document", reopened.ExtractText());
    }

    [Fact]
    public void AttachFile_from_path_uses_file_name()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".txt");
        try
        {
            File.WriteAllText(tmp, "from disk");
            using var doc = PdfFile.Open(CreateSample());
            using var result = doc.AttachFile(tmp);
            using var reopened = PdfFile.Open(result.ToBytes());

            var attachment = Assert.Single(reopened.GetAttachments());
            Assert.Equal(Path.GetFileName(tmp), attachment.Name);
            Assert.Equal("from disk", Encoding.UTF8.GetString(attachment.Content));
        }
        finally
        {
            File.Delete(tmp);
        }
    }
}
