# Managing PDFs

`PdfDocument` is immutable: manipulation methods never mutate the original — each returns a new `PdfDocument`. Dispose intermediate documents when you're done with them.

## Merge, split, and rearrange pages

```csharp
PdfFile.Merge(["a.pdf", "b.pdf"], "merged.pdf");

using var doc = PdfFile.Open("merged.pdf");
doc.Split(pagesPerFile: 1, "out/");            // one file per page
using var subset  = doc.ExtractPages(0, 2, 4);
using var trimmed = doc.DeletePages(1);
using var swapped = doc.ReorderPages(1, 0);
using var rotated = doc.RotatePage(0, Rotation.Clockwise90);
using var titled  = doc.WithMetadata(m => m.Title("New title"));
```

## Watermarks and letterheads

Stamp a page of one PDF onto another:

```csharp
using var stamp   = PdfFile.Open("watermark.pdf");
using var stamped = doc.Overlay(stamp);              // on top of the content
using var letter  = doc.Underlay(stamp);             // beneath the content
```

## File attachments

```csharp
using var withCsv = doc.AttachFile("data.csv", File.ReadAllBytes("data.csv"));
var attachments   = withCsv.GetAttachments();        // name + content
```

## XMP metadata

Apply XMP last — other manipulations regenerate it from the Info dictionary:

```csharp
using var withXmp = doc.WithGeneratedXmpMetadata();  // derived from Title/Author/…
string? xmp       = withXmp.GetXmpMetadata();        // raw packet XML
```

## Encryption

AES-256 by default; pass `EncryptionAlgorithm.Rc4_128` only if a legacy reader requires it.

```csharp
using var locked = doc.Protect(userPassword: "secret");
locked.Save("locked.pdf");

// Reopen protected files with a password — all manipulations still work:
using var opened   = PdfFile.Open("locked.pdf", password: "secret");
using var unlocked = opened.Decrypt();               // remove encryption
```

## Linearization

Linearization ("fast web view") rewrites the file so the first page loads first. Because it flattens prior incremental updates, do it **before** XMP and signing:

```csharp
using var fast = doc.Linearize();
```

## Operation ordering

> [!IMPORTANT]
> manipulations → `Linearize()` → `WithXmpMetadata()` / `AsPdfA()` → `Sign()` last.
> Linearizing flattens prior incremental updates; any rewrite after signing invalidates the signature.
