# Reading PDFs

Open a document from a path, stream, or byte array with `PdfFile.Open`. `PdfDocument` implements `IDisposable`, so wrap it in `using`.

```csharp
using var doc = PdfFile.Open("report.pdf");
Console.WriteLine(doc.PageCount);
Console.WriteLine(doc.Metadata.Title);
string allText  = doc.ExtractText();
string pageText = doc.ExtractText(0);          // 0-based
var images      = doc.GetImages(0);            // PNG bytes
```

Password-protected files open the same way — pass the password and all operations work normally:

```csharp
using var opened = PdfFile.Open("locked.pdf", password: "secret");
```

Text and image extraction is powered by [PdfPig](https://github.com/UglyToad/PdfPig). To render pages as images instead of extracting content, see [Rendering](rendering.md).
