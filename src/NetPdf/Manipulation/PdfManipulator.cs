using PdfSharp.Pdf;
using SharpDocument = PdfSharp.Pdf.PdfDocument;
using PdfSharp.Pdf.IO;

namespace NetPdf.Manipulation;

/// <summary>Page-level and document-level operations implemented over PDFsharp.</summary>
internal static class PdfManipulator
{
    internal static byte[] Merge(IEnumerable<byte[]> documents)
    {
        using var output = new SharpDocument();
        foreach (var bytes in documents)
        {
            using var input = OpenImport(bytes);
            foreach (var page in input.Pages)
                output.AddPage(page);
        }
        return ToBytes(output);
    }

    internal static IReadOnlyList<byte[]> Split(byte[] pdf, int pagesPerFile)
    {
        if (pagesPerFile < 1)
            throw new ArgumentOutOfRangeException(nameof(pagesPerFile));

        using var input = OpenImport(pdf);
        var parts = new List<byte[]>();
        for (var start = 0; start < input.PageCount; start += pagesPerFile)
        {
            using var output = new SharpDocument();
            for (var i = start; i < Math.Min(start + pagesPerFile, input.PageCount); i++)
                output.AddPage(input.Pages[i]);
            parts.Add(ToBytes(output));
        }
        return parts;
    }

    internal static byte[] ExtractPages(byte[] pdf, IEnumerable<int> pageIndexes)
    {
        using var input = OpenImport(pdf);
        using var output = new SharpDocument();
        foreach (var index in pageIndexes)
            output.AddPage(input.Pages[index]);
        return ToBytes(output);
    }

    internal static byte[] DeletePages(byte[] pdf, IReadOnlySet<int> pageIndexes)
    {
        using var input = OpenImport(pdf);
        using var output = new SharpDocument();
        for (var i = 0; i < input.PageCount; i++)
        {
            if (!pageIndexes.Contains(i))
                output.AddPage(input.Pages[i]);
        }
        return ToBytes(output);
    }

    internal static byte[] RotatePage(byte[] pdf, int pageIndex, int degreesClockwise)
    {
        using var doc = OpenModify(pdf);
        var page = doc.Pages[pageIndex];
        page.Rotate = ((page.Rotate + degreesClockwise) % 360 + 360) % 360;
        return ToBytes(doc);
    }

    internal static byte[] SetMetadata(byte[] pdf, Action<PdfDocumentInformation> configure)
    {
        using var doc = OpenModify(pdf);
        configure(doc.Info);
        return ToBytes(doc);
    }

    internal static byte[] Protect(byte[] pdf, string? userPassword, string? ownerPassword)
    {
        using var doc = OpenModify(pdf);
        var security = doc.SecuritySettings;
        // RC4-128 (V2): broadly readable, including by PdfPig for text extraction.
        doc.SecurityHandler.SetEncryptionToV2With128Bits();
        if (userPassword is not null)
            security.UserPassword = userPassword;
        if (ownerPassword is not null)
            security.OwnerPassword = ownerPassword;
        return ToBytes(doc);
    }

    private static SharpDocument OpenImport(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        return PdfSharp.Pdf.IO.PdfReader.Open(ms, PdfDocumentOpenMode.Import);
    }

    private static SharpDocument OpenModify(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        return PdfSharp.Pdf.IO.PdfReader.Open(ms, PdfDocumentOpenMode.Modify);
    }

    private static byte[] ToBytes(SharpDocument doc)
    {
        using var ms = new MemoryStream();
        doc.Save(ms, false);
        return ms.ToArray();
    }
}
