using System.Text;
using UglyToad.PdfPig;
using PigDocument = UglyToad.PdfPig.PdfDocument;

namespace NetPdf.Reading;

/// <summary>Read-only view over a PDF document: text extraction, images, metadata.</summary>
internal sealed class PdfReader : IDisposable
{
    private readonly PigDocument _document;

    internal PdfReader(byte[] pdfBytes, string? password = null)
    {
        var options = password is null
            ? new ParsingOptions()
            : new ParsingOptions { Password = password };
        _document = PigDocument.Open(pdfBytes, options);
    }

    internal int PageCount => _document.NumberOfPages;

    internal PdfMetadata Metadata
    {
        get
        {
            var info = _document.Information;
            return new PdfMetadata
            {
                Title = info.Title,
                Author = info.Author,
                Subject = info.Subject,
                Keywords = info.Keywords,
                Creator = info.Creator,
                Producer = info.Producer,
            };
        }
    }

    internal IReadOnlyList<PdfAttachment> GetAttachments()
    {
        if (!_document.Advanced.TryGetEmbeddedFiles(out var files))
            return [];
        return files
            .Select(f => new PdfAttachment { Name = f.Name, Content = f.Memory.ToArray() })
            .ToList();
    }

    internal string ExtractText()
    {
        var sb = new StringBuilder();
        for (var i = 1; i <= _document.NumberOfPages; i++)
        {
            if (i > 1)
                sb.AppendLine();
            sb.Append(_document.GetPage(i).Text);
        }
        return sb.ToString();
    }

    internal string ExtractText(int pageIndex) => _document.GetPage(pageIndex + 1).Text;

    internal IReadOnlyList<byte[]> GetImages(int pageIndex)
    {
        var images = new List<byte[]>();
        foreach (var image in _document.GetPage(pageIndex + 1).GetImages())
        {
            if (image.TryGetPng(out var png))
                images.Add(png);
            else
                images.Add(image.RawBytes.ToArray());
        }
        return images;
    }

    public void Dispose() => _document.Dispose();
}
