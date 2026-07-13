# Compliance: PDF/A, tagged PDF, and RTL

## Tagged PDF (accessibility) and PDF/A

`WithTagging()` builds a structure tree (headings, paragraphs, figures with alt text) for assistive technologies; `AsPdfA()` adds an sRGB output intent and PDF/A identification XMP for PDF/A-2b conformance.

```csharp
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
```

## Right-to-left layout

RTL content gets bidi reordering and Arabic shaping. Enable it for the whole page or per container:

```csharp
Document.Create(doc => doc
    .Page(page => page
        .ContentFromRightToLeft()
        .Content(c => c.Text("مرحبا بالعالم"))));

// …or per container: c.ContentFromRightToLeft().Row(…)
```

> [!NOTE]
> Rendering Arabic requires a font with presentation-form glyphs (e.g. Arial, Noto Naskh Arabic, Amiri).

## Ordering with other operations

`AsPdfA()` and XMP generation belong near the end of the pipeline, but before signing:

manipulations → `Linearize()` → `WithXmpMetadata()` / `AsPdfA()` → `Sign()` last.
