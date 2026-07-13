# Getting started

## Install

```sh
dotnet add package NetPdfKit
```

The package targets `net8.0`, `net9.0`, and `net10.0` and works on Windows, macOS, and Linux. Fonts are resolved from the operating system's font directories on all platforms — on minimal Linux containers, install a font package such as `fonts-liberation` or `fonts-dejavu-core`.

## Create your first PDF

The coordinate-based builder gives you precise control. Coordinates are PDF points (1/72 inch) measured from the top-left corner, and page indexes are 0-based everywhere.

```csharp
using NetPdf;

PdfFile.Create()
    .WithMetadata(m => m.Title("Report").Author("Me"))
    .AddPage(page => page
        .AddText("Hello, world", x: 50, y: 50, o => o.FontSize(24).Bold())
        .AddText("Wrapped body text…", x: 50, y: 100, o => o.Wrap(400))
        .AddImage("logo.png", x: 50, y: 200, width: 150)
        .AddRectangle(50, 400, 200, 80, fill: System.Drawing.Color.LightGray)
        .AddEllipse(300, 400, 100, 60, fill: System.Drawing.Color.LightBlue)
        .AddRoundedRectangle(50, 500, 200, 60, cornerRadius: 8)
        .AddWebLink(50, 50, 200, 24, "https://example.com/"))
    .WithOutline(o => o
        .AddBookmark("Introduction", 0))
    .Save("report.pdf");
```

Text supports styling and layout options: `Bold()`, `Italic()`, `Underline()`, `Strikethrough()`, `Align(TextAlignment.Center)`, `Wrap(width)`, and `LineSpacing(1.5)`.

## Two ways to author documents

NetPdf has two authoring models — pick the one that fits:

1. **Coordinate drawing** (`PdfFile.Create()`, shown above) — you place every element at explicit x/y positions. Best for stamps, forms, and pixel-precise output.
2. **Fluent layout** (`Document.Create(...)`) — content is measured and paginated automatically, with repeating headers/footers and page numbers. Best for reports and flowing documents. See [Fluent layout](fluent-layout.md).

## Next steps

- [Fluent layout](fluent-layout.md) — auto-paginated documents
- [Reading PDFs](reading.md) — open and extract content
- [Managing PDFs](managing.md) — merge, split, encrypt, and more
