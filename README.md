# NetPdf

![GitHub stars](https://shieldcn.dev/github/stars/edcsu/netpdf)
![GitHub release](https://shieldcn.dev/github/release/edcsu/netpdf)
![GitHub CI](https://shieldcn.dev/github/ci/edcsu/netpdf)
![GitHub license](https://shieldcn.dev/github/license/edcsu/netpdf)
![GitHub issues](https://shieldcn.dev/github/issues/edcsu/netpdf)
![GitHub PRs](https://shieldcn.dev/github/prs/edcsu/netpdf)
![GitHub commits](https://shieldcn.dev/github/commits/edcsu/netpdf)

Create, view, and manage PDF files in .NET with one fluent API.

NetPdf wraps three battle-tested, permissively-licensed libraries behind a single coherent surface:

| Capability | Powered by | License |
|---|---|---|
| Create, merge, split, rotate, encrypt | [PDFsharp](https://github.com/empira/PDFsharp) | MIT |
| Text & image extraction | [PdfPig](https://github.com/UglyToad/PdfPig) | Apache-2.0 |
| Render pages to PNG | [PDFtoImage](https://github.com/sungaila/PDFtoImage) (PDFium) | MIT |
| Barcodes & QR codes | [ZXing.Net](https://github.com/micjahn/ZXing.Net) | Apache-2.0 |
| SVG rasterization | [Svg.Skia](https://github.com/wieslawsoltes/Svg.Skia) (SkiaSharp) | MIT |

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
        .AddRectangle(50, 400, 200, 80, fill: System.Drawing.Color.LightGray)
        .AddEllipse(300, 400, 100, 60, fill: System.Drawing.Color.LightBlue)
        .AddRoundedRectangle(50, 500, 200, 60, cornerRadius: 8)
        .AddWebLink(50, 50, 200, 24, "https://example.com/"))
    .WithOutline(o => o
        .AddBookmark("Introduction", 0))
    .Save("report.pdf");
```

Text supports styling and layout options: `Bold()`, `Italic()`, `Underline()`, `Strikethrough()`,
`Align(TextAlignment.Center)`, `Wrap(width)`, and `LineSpacing(1.5)`.

## Lay out a document

The fluent layout API measures and paginates content automatically — no coordinates needed.
Headers and footers repeat on every page, and `{number}`/`{total}` resolve to page numbers.

```csharp
using NetPdf.Fluent;

Document.Create(doc => doc
    .Page(page => page
        .Size(PageSizes.A4)
        .Margin(50)
        .Header(h => h.Text("ACME Quarterly Report"))
        .Content(c => c
            .Padding(10)
            .Column(column =>
            {
                column.Spacing(8);
                column.Item().Text("Body text wraps and flows across pages automatically.");
                column.Item().Row(row =>
                {
                    row.ConstantItem(120).Text("Label");
                    row.RelativeItem().Text("Value that takes the remaining width");
                });
            }))
        .Footer(f => f.AlignCenter().PageNumber("Page {number} of {total}"))))
    .Save("report.pdf");
```

Containers compose with chainable calls: `Padding`, `Width`/`Height` (plus min/max),
`AlignCenter`/`AlignMiddle`/…, `AspectRatio`, `Extend`, `Shrink`, `Unconstrained`, and `Offset`.
Custom `IElement` implementations plug in via `.Element(...)`.

Content flow is controlled per block: `ShowEntire` keeps a block on one page, `EnsureSpace(pt)`
requires a minimum height before starting, `PageBreak()` forces a new page, `StopPaging`
truncates to the current page, and `ShowOnce`/`SkipOnce`/`ShowIf(bool)`/`Repeat(...)` control
repeated slots. `DefaultTextStyle(style)` cascades text styling to everything inside.

Rich text mixes styles, hyperlinks, and paragraph settings inside one flowing block, and
`List`/`Inlined`/`Decoration` cover common structures:

```csharp
content.Column(column =>
{
    column.Item().Text(text =>
    {
        text.LineHeight(1.4);
        text.Span("NetPdf ").Bold();
        text.Span("builds rich paragraphs — ");
        text.Hyperlink("learn more", "https://example.com");
        text.Span(".");
    });
    column.Item().List(list =>
    {
        list.Ordered().Spacing(4);
        list.Item().Text("First point");
        list.Item().Text("Second point");
    });
});
```

Tables have fixed column widths, cell spans, and header/footer rows that repeat on every page;
a spanned cell never splits across a page break:

```csharp
content.Table(table =>
{
    table.ColumnsDefinition(columns =>
    {
        columns.ConstantColumn(120);
        columns.RelativeColumn();
    });
    table.Header(header =>
    {
        header.Cell().Text("Name");
        header.Cell().Text("Description");
    });
    table.Cell().Text("Item 1");
    table.Cell().Text("First item");
    table.Cell().Row(2).Column(1).RowSpan(2).Text("Spans two rows");
});
```

Visual styling and transforms chain onto any container: `Background(color, cornerRadius)`,
`Border(...)`, `Shadow(...)` (blur is approximated — PDF has no native blur), `Rotate(degrees)`,
`Scale(x, y)`, `FlipHorizontal`/`FlipVertical`/`FlipOver`, and `ScaleToFit()` which shrinks
content until it fits its slot. Layers accept a `zIndex` to control stacking order.

QR codes, barcodes, and SVG render through the same image pipeline, and `Canvas` exposes raw
drawing for charts and custom graphics:

```csharp
content.Column(column =>
{
    column.Item().Width(120).QrCode("https://example.com");
    column.Item().Width(240).Barcode("NETPDF-12345", BarcodeFormat.Code128);
    column.Item().Width(80).Svg("<svg …>…</svg>");             // rasterized at 2× by default
    column.Item().Height(100).Canvas((canvas, size) =>
        canvas.DrawLine(0, size.Height, size.Width, 0, Color.SteelBlue, 2));
});
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

// Watermarks / letterheads: stamp a page of one PDF onto another
using var stamp     = PdfFile.Open("watermark.pdf");
using var stamped   = doc.Overlay(stamp);              // on top of the content
using var letter    = doc.Underlay(stamp);             // beneath the content

// File attachments
using var withCsv   = doc.AttachFile("data.csv", File.ReadAllBytes("data.csv"));
var attachments     = withCsv.GetAttachments();        // name + content

// XMP metadata (apply last — other manipulations regenerate it from the Info dictionary)
using var withXmp   = doc.WithGeneratedXmpMetadata();  // derived from Title/Author/…
string? xmp         = withXmp.GetXmpMetadata();        // raw packet XML

// Encryption (AES-256 by default; Rc4_128 available for legacy readers)
using var locked  = doc.Protect(userPassword: "secret");
locked.Save("locked.pdf");

// Reopen protected files with a password — all manipulations still work:
using var opened    = PdfFile.Open("locked.pdf", password: "secret");
using var unlocked  = opened.Decrypt();                // remove encryption
```

## Compliance & long tail

```csharp
// Tagged PDF (accessibility) + PDF/A-2b
var bytes = Document.Create(doc => doc
        .Page(page => page.Content(c => c.Column(col =>
        {
            col.Item().Heading(1).Text("Title");
            col.Item().Paragraph().Text("Body text");
            col.Item().Image(logoPng, altText: "Company logo");
        }))))
    .WithTagging()   // structure tree: headings, paragraphs, figures with alt text
    .AsPdfA()        // sRGB output intent + pdfaid identification XMP
    .ToBytes();

// Right-to-left layout with bidi reordering and Arabic shaping
Document.Create(doc => doc
    .Page(page => page
        .ContentFromRightToLeft()
        .Content(c => c.Text("مرحبا بالعالم"))));
// …or per container: c.ContentFromRightToLeft().Row(…)

// Digital signature (detached PKCS#7) — always the LAST operation
using var doc2   = PdfFile.Open(bytes);
using var signed = doc2.Sign(certificate, new SignatureOptions { Reason = "Approved" });
var sigs = signed.GetSignatures();   // SignerSubject, IsIntact, CoversWholeDocument

// Linearization ("fast web view") — rewrites the file, so do it before XMP/signing
using var fast = doc2.Linearize();

// Layout debugging: outline any element, or all page slots, then render to PNG
Document.Create(doc => doc
    .Page(page => page
        .DebugOverlay()                       // outlines header/content/footer
        .Content(c => c.Debug("body").Text("…"))));
```

**Operation ordering:** manipulations → `Linearize()` → `WithXmpMetadata()` / `AsPdfA()` → `Sign()` last. Linearizing flattens prior incremental updates; any rewrite after signing invalidates the signature.

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
- `Protect` uses AES-256 by default; pass `EncryptionAlgorithm.Rc4_128` only if a legacy reader requires it.
- Rendering Arabic requires a font with presentation-form glyphs (e.g. Arial, Noto Naskh Arabic, Amiri).
- Linearization writes simplified (structurally valid) hint tables; the practical benefit is first-page-first ordering.
- Signature verification checks integrity only; certificate trust chains are not evaluated.

## License

MIT
