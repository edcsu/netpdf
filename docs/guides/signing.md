# Digital signing

NetPdf signs documents with a detached PKCS#7 (CMS) signature written as an incremental update, using any `X509Certificate2` with a private key.

```csharp
using System.Security.Cryptography.X509Certificates;
using NetPdf;

var certificate = X509CertificateLoader.LoadPkcs12FromFile("signer.pfx", "pfx-password");

using var doc    = PdfFile.Open("report.pdf");
using var signed = doc.Sign(certificate, new SignatureOptions { Reason = "Approved" });
signed.Save("report-signed.pdf");
```

`SignatureOptions` also accepts `Location`, `ContactInfo`, `Name`, and `SigningTime`.

## Verifying signatures

```csharp
var sigs = signed.GetSignatures();
foreach (var sig in sigs)
{
    Console.WriteLine(sig.SignerSubject);
    Console.WriteLine(sig.IsIntact);              // digest matches the signed bytes
    Console.WriteLine(sig.CoversWholeDocument);   // no content added after signing
}
```

> [!NOTE]
> Signature verification checks integrity only; certificate trust chains are not evaluated.

## Sign last

Signing must be the **final** operation. Any rewrite after signing — including linearization, XMP regeneration, or PDF/A conversion — invalidates the signature:

manipulations → `Linearize()` → `WithXmpMetadata()` / `AsPdfA()` → `Sign()` last.
