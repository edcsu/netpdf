# NetPdf

Create, view, and manage PDF files in .NET with one fluent API.

NetPdf (published on NuGet as **NetPdfKit**) wraps battle-tested, permissively-licensed libraries — PDFsharp, PdfPig, PDFtoImage, ZXing.Net, and Svg.Skia — behind a single coherent document model. It targets `net8.0`, `net9.0`, and `net10.0` and works on Windows, macOS, and Linux.

## Install

```sh
dotnet add package NetPdfKit
```

## Hello, PDF

```csharp
using NetPdf;

PdfFile.Create()
    .WithMetadata(m => m.Title("Report").Author("Me"))
    .AddPage(page => page
        .AddText("Hello, world", x: 50, y: 50, o => o.FontSize(24).Bold()))
    .Save("report.pdf");
```

## Where to go next

- [Getting started](guides/getting-started.md) — install and create your first PDF
- [Fluent layout](guides/fluent-layout.md) — auto-paginated documents, tables, rich text, lists
- [Reading PDFs](guides/reading.md) — text and image extraction, metadata
- [Managing PDFs](guides/managing.md) — merge, split, overlay, attachments, encryption
- [Digital signing](guides/signing.md) — sign and verify with X.509 certificates
- [Compliance](guides/compliance.md) — PDF/A, tagged (accessible) PDF, XMP, right-to-left text
- [Rendering](guides/rendering.md) — render pages to PNG images
- [Architecture](guides/architecture.md) — how the pieces fit together
- [API Reference](api/index.md) — generated from the source XML documentation
