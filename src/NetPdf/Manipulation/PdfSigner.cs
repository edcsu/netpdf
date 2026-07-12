using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using NetPdf.Reading;

namespace NetPdf.Manipulation;

/// <summary>
/// Applies a detached PKCS#7 (CMS) digital signature to a PDF as an incremental update:
/// a /Sig dictionary with /ByteRange and /Contents, an invisible signature widget on the
/// first page, and an /AcroForm entry on the catalog. Signing must be the last operation
/// on a document — any later rewrite invalidates the signature.
/// </summary>
internal static partial class PdfSigner
{
    /// <summary>Reserved space for the DER-encoded CMS blob, in bytes (hex doubles it).</summary>
    private const int ContentsCapacity = 16384;

    [GeneratedRegex(@"/Pages\s+(\d+)\s+0\s+R")]
    private static partial Regex PagesRefRegex();

    [GeneratedRegex(@"/Kids\s*\[\s*(\d+)\s+0\s+R")]
    private static partial Regex FirstKidRegex();

    [GeneratedRegex(@"/Annots\s*\[")]
    private static partial Regex AnnotsArrayRegex();

    [GeneratedRegex(@"/AcroForm[^/>]*")]
    private static partial Regex AcroFormRegex();

    internal static byte[] Sign(byte[] pdf, X509Certificate2 certificate, SignatureOptions options)
    {
        if (!certificate.HasPrivateKey)
            throw new ArgumentException("The certificate has no private key.", nameof(certificate));

        var update = new IncrementalUpdate(pdf);
        var trailer = update.Trailer;

        // Locate the first page object through catalog → /Pages → first /Kids entry.
        var catalogDict = IncrementalUpdate.FindObjectBody(trailer, trailer.RootNumber);
        var pagesRef = PagesRefRegex().Match(catalogDict);
        if (!pagesRef.Success)
            throw new InvalidOperationException("Unsupported PDF structure: /Pages reference not found.");
        var pagesDict = IncrementalUpdate.FindObjectBody(trailer, int.Parse(pagesRef.Groups[1].Value));
        var firstKid = FirstKidRegex().Match(pagesDict);
        if (!firstKid.Success)
            throw new InvalidOperationException("Unsupported PDF structure: page tree has no kids.");
        var firstPageNum = int.Parse(firstKid.Groups[1].Value);
        var pageDict = IncrementalUpdate.FindObjectBody(trailer, firstPageNum);

        var sigNum = update.NextObjectNumber;
        var widgetNum = sigNum + 1;

        var signingTime = options.SigningTime ?? DateTimeOffset.Now;
        var name = options.Name ?? certificate.GetNameInfo(X509NameType.SimpleName, false);
        var sb = new StringBuilder();
        sb.Append("<< /Type /Sig /Filter /Adobe.PPKLite /SubFilter /adbe.pkcs7.detached");
        sb.Append($" /M ({FormatPdfDate(signingTime)})");
        if (name is { Length: > 0 })
            sb.Append($" /Name ({EscapeString(name)})");
        if (options.Reason is { } reason)
            sb.Append($" /Reason ({EscapeString(reason)})");
        if (options.Location is { } location)
            sb.Append($" /Location ({EscapeString(location)})");
        if (options.ContactInfo is { } contact)
            sb.Append($" /ContactInfo ({EscapeString(contact)})");
        sb.Append(" /ByteRange [0 0000000000 0000000000 0000000000]");
        sb.Append($" /Contents <{new string('0', ContentsCapacity * 2)}>");
        sb.Append(" >>");
        update.AddObject(sigNum, sb.ToString());

        update.AddObject(widgetNum,
            $"<< /Type /Annot /Subtype /Widget /FT /Sig /Rect [0 0 0 0] /F 132 " +
            $"/T (Signature1) /V {sigNum} 0 R /P {firstPageNum} 0 R >>");

        // Re-emit the first page with the widget appended to /Annots.
        var widgetRef = $"{widgetNum} 0 R";
        var newPageDict = AnnotsArrayRegex().IsMatch(pageDict)
            ? AnnotsArrayRegex().Replace(pageDict, $"/Annots [{widgetRef} ", 1)
            : pageDict.Insert(2, $" /Annots [{widgetRef}] ");
        update.AddObject(firstPageNum, newPageDict);

        // Re-emit the catalog with /AcroForm (replacing an existing entry if present).
        var acroForm = $"/AcroForm << /Fields [{widgetNum} 0 R] /SigFlags 3 >>";
        var newCatalog = AcroFormRegex().IsMatch(catalogDict)
            ? AcroFormRegex().Replace(catalogDict, acroForm, 1)
            : catalogDict.Insert(2, $" {acroForm} ");
        update.AddObject(trailer.RootNumber, newCatalog);

        var bytes = update.Complete();
        return EmbedSignature(bytes, certificate, options);
    }

    /// <summary>Patches /ByteRange, hashes the covered ranges, and embeds the detached CMS blob.</summary>
    private static byte[] EmbedSignature(byte[] bytes, X509Certificate2 certificate, SignatureOptions options)
    {
        var text = Encoding.Latin1.GetString(bytes);
        var placeholder = "/Contents <" + new string('0', ContentsCapacity * 2);
        var contentsIndex = text.LastIndexOf(placeholder, StringComparison.Ordinal);
        if (contentsIndex < 0)
            throw new InvalidOperationException("Signature placeholder not found.");
        var hexStart = contentsIndex + "/Contents <".Length;
        var hexEnd = hexStart + ContentsCapacity * 2;

        // ByteRange covers everything except the hex string including its <> delimiters.
        long r1Length = hexStart - 1;
        long r2Start = hexEnd + 1;
        long r2Length = bytes.Length - r2Start;
        var byteRange = $"/ByteRange [0 {r1Length:0000000000} {r2Start:0000000000} {r2Length:0000000000}]";
        var byteRangeIndex = text.LastIndexOf("/ByteRange [0 0000000000 0000000000 0000000000]", StringComparison.Ordinal);
        if (byteRangeIndex < 0)
            throw new InvalidOperationException("ByteRange placeholder not found.");
        Encoding.Latin1.GetBytes(byteRange).CopyTo(bytes, byteRangeIndex);

        // Hash the two covered ranges and produce the detached CMS signature.
        var covered = new byte[r1Length + r2Length];
        Array.Copy(bytes, 0, covered, 0, r1Length);
        Array.Copy(bytes, r2Start, covered, r1Length, r2Length);
        var cms = new SignedCms(new ContentInfo(covered), detached: true);
        var signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, certificate)
        {
            DigestAlgorithm = new Oid("2.16.840.1.101.3.4.2.1"), // SHA-256
        };
        cms.ComputeSignature(signer);
        var der = cms.Encode();
        if (der.Length > ContentsCapacity)
            throw new InvalidOperationException(
                $"Signature ({der.Length} bytes) exceeds the reserved {ContentsCapacity}-byte placeholder.");

        var hex = Convert.ToHexString(der);
        Encoding.Latin1.GetBytes(hex).CopyTo(bytes, hexStart);
        return bytes;
    }

    /// <summary>Reads all signature dictionaries in the file and verifies their integrity.</summary>
    internal static IReadOnlyList<PdfSignatureInfo> ReadSignatures(byte[] pdf)
    {
        var text = Encoding.Latin1.GetString(pdf);
        var results = new List<PdfSignatureInfo>();
        foreach (Match m in Regex.Matches(text,
            @"/Type\s*/Sig.*?/ByteRange\s*\[\s*(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s*\].*?/Contents\s*<([0-9A-Fa-f]+)>",
            RegexOptions.Singleline))
        {
            long r1Start = long.Parse(m.Groups[1].Value);
            long r1Length = long.Parse(m.Groups[2].Value);
            long r2Start = long.Parse(m.Groups[3].Value);
            long r2Length = long.Parse(m.Groups[4].Value);
            var hex = m.Groups[5].Value.TrimEnd('0');
            if (hex.Length % 2 == 1)
                hex += "0";

            string? signerSubject = null;
            DateTimeOffset? signingTime = null;
            bool intact = false;
            try
            {
                var der = Convert.FromHexString(hex);
                var covered = new byte[r1Length + r2Length];
                Array.Copy(pdf, r1Start, covered, 0, r1Length);
                Array.Copy(pdf, r2Start, covered, r1Length, r2Length);
                var cms = new SignedCms(new ContentInfo(covered), detached: true);
                cms.Decode(der);
                signerSubject = cms.SignerInfos.Count > 0
                    ? cms.SignerInfos[0].Certificate?.Subject
                    : null;
                cms.CheckSignature(verifySignatureOnly: true);
                intact = true;
            }
            catch (CryptographicException)
            {
                // Leaves intact = false; the signature is present but does not verify.
            }

            var timeMatch = Regex.Match(m.Value, @"/M\s*\(D:(\d{14})");
            if (timeMatch.Success && DateTimeOffset.TryParseExact(timeMatch.Groups[1].Value,
                    "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.AssumeLocal, out var parsed))
                signingTime = parsed;

            results.Add(new PdfSignatureInfo
            {
                SignerSubject = signerSubject,
                SigningTime = signingTime,
                SubFilter = "adbe.pkcs7.detached",
                CoversWholeDocument = r2Start + r2Length == pdf.Length,
                IsIntact = intact,
            });
        }
        return results;
    }

    private static string FormatPdfDate(DateTimeOffset time)
    {
        var offset = time.Offset;
        var sign = offset < TimeSpan.Zero ? '-' : '+';
        return $"D:{time:yyyyMMddHHmmss}{sign}{Math.Abs(offset.Hours):00}'{Math.Abs(offset.Minutes):00}'";
    }

    private static string EscapeString(string value) =>
        value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
}
