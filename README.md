# NetPdf

Create, view, and manage PDF files in .NET with one fluent API.

NetPdf wraps three battle-tested, permissively-licensed libraries behind a single coherent surface:

| Capability | Powered by | License |
|---|---|---|
| Create, merge, split, rotate, encrypt | [PDFsharp](https://github.com/empira/PDFsharp) | MIT |
| Text & image extraction | [PdfPig](https://github.com/UglyToad/PdfPig) | Apache-2.0 |
| Render pages to PNG | [PDFtoImage](https://github.com/sungaila/PDFtoImage) (PDFium) | MIT |

Targets `net8.0`, `net9.0`, and `net10.0`. Works on Windows, macOS, and Linux.

## Install

```sh
dotnet add package NetPdfKit
```

## Create a PDF

```csharp
using NetPdf;

PdfFile.Create()
    .WithMetadata(m => m.Title("Report").Author("Me"))
    .AddPage(page => page
        .AddText("Hello, world", x: 50, y: 50, o => o.FontSize(24).Bold())
        .AddText("Wrapped body text…", x: 50, y: 100, o => o.Wrap(400))
        .AddImage("logo.png", x: 50, y: 200, width: 150)
        .AddRectangle(50, 400, 200, 80, fill: System.Drawing.Color.LightGray))
    .Save("report.pdf");
```

## Read a PDF

```csharp
using var doc = PdfFile.Open("report.pdf");
Console.WriteLine(doc.PageCount);
Console.WriteLine(doc.Metadata.Title);
string allText  = doc.ExtractText();
string pageText = doc.ExtractText(0);          // 0-based
var images      = doc.GetImages(0);            // PNG bytes
```

## Manage PDFs

Manipulation methods never mutate the original — each returns a new `PdfDocument`.

```csharp
PdfFile.Merge(["a.pdf", "b.pdf"], "merged.pdf");

using var doc = PdfFile.Open("merged.pdf");
doc.Split(pagesPerFile: 1, "out/");            // one file per page
using var subset  = doc.ExtractPages(0, 2, 4);
using var trimmed = doc.DeletePages(1);
using var swapped = doc.ReorderPages(1, 0);
using var rotated = doc.RotatePage(0, Rotation.Clockwise90);
using var titled  = doc.WithMetadata(m => m.Title("New title"));
using var locked  = doc.Protect(userPassword: "secret");
locked.Save("locked.pdf");

// Reopen protected files with a password:
using var unlocked = PdfFile.Open("locked.pdf", password: "secret");
```

## View (render to image)

```csharp
using var doc = PdfFile.Open("report.pdf");
byte[] png = doc.RenderPage(0, dpi: 150);
File.WriteAllBytes("page1.png", png);

var allPages = doc.RenderAllPages(dpi: 96);
```

## Notes

- Page indexes are 0-based everywhere; coordinates are PDF points (1/72 inch) from the top-left corner.
- Fonts are resolved from the operating system's font directories on all platforms.
- `Protect` uses RC4-128 so protected files remain readable by common extractors; for stronger needs, open an issue.

## License

MIT
