using PdfSharp.Drawing;
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

    internal static IReadOnlyList<byte[]> Split(byte[] pdf, int pagesPerFile, string? password = null)
    {
        if (pagesPerFile < 1)
            throw new ArgumentOutOfRangeException(nameof(pagesPerFile));

        using var input = OpenImport(pdf, password);
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

    internal static byte[] ExtractPages(byte[] pdf, IEnumerable<int> pageIndexes, string? password = null)
    {
        using var input = OpenImport(pdf, password);
        using var output = new SharpDocument();
        foreach (var index in pageIndexes)
            output.AddPage(input.Pages[index]);
        return ToBytes(output);
    }

    internal static byte[] DeletePages(byte[] pdf, IReadOnlySet<int> pageIndexes, string? password = null)
    {
        using var input = OpenImport(pdf, password);
        using var output = new SharpDocument();
        for (var i = 0; i < input.PageCount; i++)
        {
            if (!pageIndexes.Contains(i))
                output.AddPage(input.Pages[i]);
        }
        return ToBytes(output);
    }

    internal static byte[] RotatePage(byte[] pdf, int pageIndex, int degreesClockwise, string? password = null)
    {
        using var doc = OpenModify(pdf, password);
        var page = doc.Pages[pageIndex];
        page.Rotate = ((page.Rotate + degreesClockwise) % 360 + 360) % 360;
        return ToBytes(doc);
    }

    internal static byte[] SetMetadata(byte[] pdf, Action<PdfDocumentInformation> configure, string? password = null)
    {
        using var doc = OpenModify(pdf, password);
        configure(doc.Info);
        return ToBytes(doc);
    }

    internal static byte[] Protect(byte[] pdf, string? userPassword, string? ownerPassword,
        EncryptionAlgorithm algorithm, string? openPassword)
    {
        using var doc = OpenModify(pdf, openPassword);
        var security = doc.SecuritySettings;
        switch (algorithm)
        {
            case EncryptionAlgorithm.Rc4_128:
                doc.SecurityHandler.SetEncryptionToV2With128Bits();
                break;
            default:
                // V5: AES-256 (PDF 2.0).
                doc.SecurityHandler.SetEncryptionToV5();
                break;
        }
        if (userPassword is not null)
            security.UserPassword = userPassword;
        if (ownerPassword is not null)
            security.OwnerPassword = ownerPassword;
        return ToBytes(doc);
    }

    internal static byte[] Decrypt(byte[] pdf, string? password)
    {
        using var doc = OpenModify(pdf, password);
        doc.SecurityHandler.SetEncryptionToNoneAndResetPasswords();
        return ToBytes(doc);
    }

    internal static byte[] Stamp(byte[] pdf, byte[] stamp, int stampPageIndex,
        IReadOnlySet<int> pageIndexes, bool under, string? password = null)
    {
        using var doc = OpenModify(pdf, password);
        // The source stream must stay alive until the document is saved.
        using var stampStream = new MemoryStream(stamp);
        using var form = XPdfForm.FromStream(stampStream);
        if (stampPageIndex < 0 || stampPageIndex >= form.PageCount)
            throw new ArgumentOutOfRangeException(nameof(stampPageIndex));
        form.PageIndex = stampPageIndex;

        for (var i = 0; i < doc.PageCount; i++)
        {
            if (pageIndexes.Count > 0 && !pageIndexes.Contains(i))
                continue;
            var page = doc.Pages[i];
            using var gfx = XGraphics.FromPdfPage(page,
                under ? XGraphicsPdfPageOptions.Prepend : XGraphicsPdfPageOptions.Append);
            gfx.DrawImage(form, new XRect(0, 0, page.Width.Point, page.Height.Point));
        }
        return ToBytes(doc);
    }

    internal static byte[] AttachFile(byte[] pdf, string name, byte[] content, string? password = null)
    {
        // Built by hand: PDFsharp's AddEmbeddedFile replaces the name tree of a loaded
        // document instead of appending to it, dropping existing attachments.
        using var doc = OpenModify(pdf, password);

        var efStream = new PdfDictionary(doc);
        doc.Internals.AddObject(efStream);
        efStream.Elements["/Type"] = new PdfName("/EmbeddedFile");
        efStream.CreateStream(content);

        var efDict = new PdfDictionary(doc);
        efDict.Elements["/F"] = efStream.Reference;

        var filespec = new PdfDictionary(doc);
        doc.Internals.AddObject(filespec);
        filespec.Elements["/Type"] = new PdfName("/Filespec");
        filespec.Elements["/F"] = new PdfString(name);
        filespec.Elements["/UF"] = new PdfString(name);
        filespec.Elements["/EF"] = efDict;

        var catalog = doc.Internals.Catalog;
        var names = catalog.Elements.GetDictionary("/Names");
        if (names is null)
        {
            names = new PdfDictionary(doc);
            catalog.Elements["/Names"] = names;
        }
        var embedded = names.Elements.GetDictionary("/EmbeddedFiles");
        if (embedded is null)
        {
            embedded = new PdfDictionary(doc);
            names.Elements["/EmbeddedFiles"] = embedded;
        }
        var entries = embedded.Elements.GetArray("/Names");
        if (entries is null)
        {
            entries = new PdfArray(doc);
            embedded.Elements["/Names"] = entries;
        }
        entries.Elements.Add(new PdfString(name));
        entries.Elements.Add(filespec.Reference!);
        return ToBytes(doc);
    }

    private static SharpDocument OpenImport(byte[] bytes, string? password = null)
    {
        using var ms = new MemoryStream(bytes);
        return password is null
            ? PdfSharp.Pdf.IO.PdfReader.Open(ms, PdfDocumentOpenMode.Import)
            : PdfSharp.Pdf.IO.PdfReader.Open(ms, password, PdfDocumentOpenMode.Import);
    }

    private static SharpDocument OpenModify(byte[] bytes, string? password = null)
    {
        using var ms = new MemoryStream(bytes);
        return password is null
            ? PdfSharp.Pdf.IO.PdfReader.Open(ms, PdfDocumentOpenMode.Modify)
            : PdfSharp.Pdf.IO.PdfReader.Open(ms, password, PdfDocumentOpenMode.Modify);
    }

    private static byte[] ToBytes(SharpDocument doc)
    {
        using var ms = new MemoryStream();
        doc.Save(ms, false);
        return ms.ToArray();
    }
}
