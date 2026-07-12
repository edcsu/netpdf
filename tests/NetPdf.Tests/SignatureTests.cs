using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using NetPdf.Fluent;
using Xunit;

namespace NetPdf.Tests;

public class SignatureTests
{
    private static X509Certificate2 CreateTestCertificate() =>
        TestCertificates.CreateEphemeral("CN=NetPdf Test Signer");

    private static byte[] BuildSamplePdf() =>
        Document.Create(doc => doc
                .Page(page => page.Content(c => c.Text("Signed document"))))
            .ToBytes();

    [Fact]
    public void Sign_EmbedsDetachedPkcs7Signature()
    {
        using var cert = CreateTestCertificate();
        using var pdf = PdfFile.Open(BuildSamplePdf());

        var signed = pdf.Sign(cert, new SignatureOptions { Reason = "Approval", Location = "Kampala" });

        var text = Encoding.Latin1.GetString(signed.ToBytes());
        Assert.Contains("/ByteRange", text);
        Assert.Contains("/adbe.pkcs7.detached", text);
        Assert.Contains("/Reason (Approval)", text);
        Assert.Contains("/Location (Kampala)", text);
    }

    [Fact]
    public void GetSignatures_VerifiesIntactSignature()
    {
        using var cert = CreateTestCertificate();
        using var pdf = PdfFile.Open(BuildSamplePdf());

        var signed = pdf.Sign(cert);
        var signatures = signed.GetSignatures();

        var sig = Assert.Single(signatures);
        Assert.True(sig.IsIntact);
        Assert.True(sig.CoversWholeDocument);
        Assert.Contains("NetPdf Test Signer", sig.SignerSubject);
        Assert.NotNull(sig.SigningTime);
    }

    [Fact]
    public void GetSignatures_VerifiesIntactSignature_WhenDerEndsInZeroNibble()
    {
        // Regression test: the /Contents hex placeholder is zero-padded, and the reader
        // used to recover the real DER blob by trimming trailing '0' characters. That
        // corrupts the signature whenever the real (random) DER bytes happen to end in a
        // zero nibble. Retry signing until that condition occurs, so the case is exercised
        // deterministically instead of relying on rare CI flakes.
        for (var attempt = 0; attempt < 200; attempt++)
        {
            using var cert = CreateTestCertificate();
            using var pdf = PdfFile.Open(BuildSamplePdf());
            var signed = pdf.Sign(cert);
            var bytes = signed.ToBytes();
            var text = Encoding.Latin1.GetString(bytes);
            var match = System.Text.RegularExpressions.Regex.Match(text, @"/Contents\s*<([0-9A-Fa-f]+)>");
            Assert.True(match.Success);

            var full = Convert.FromHexString(match.Groups[1].Value);
            var derLength = GetDerSequenceLength(full);
            if (full[derLength - 1] % 16 != 0)
                continue; // Last byte doesn't end in a zero nibble; try another key/signature.

            var sig = Assert.Single(signed.GetSignatures());
            Assert.True(sig.IsIntact);
            return;
        }

        Assert.Fail("Could not produce a DER signature ending in a zero nibble within 200 attempts.");
    }

    private static int GetDerSequenceLength(byte[] data)
    {
        Assert.Equal(0x30, data[0]);
        byte first = data[1];
        if (first < 0x80)
            return 2 + first;
        int lengthOctets = first & 0x7F;
        int contentLength = 0;
        for (var i = 0; i < lengthOctets; i++)
            contentLength = (contentLength << 8) | data[2 + i];
        return 2 + lengthOctets + contentLength;
    }

    [Fact]
    public void GetSignatures_DetectsTampering()
    {
        using var cert = CreateTestCertificate();
        using var pdf = PdfFile.Open(BuildSamplePdf());
        var bytes = pdf.Sign(cert).ToBytes();

        // Flip a byte inside the signed region (near the start of the content stream).
        bytes[200] ^= 0xFF;

        using var tampered = PdfFile.Open(bytes);
        var sig = Assert.Single(tampered.GetSignatures());
        Assert.False(sig.IsIntact);
    }

    [Fact]
    public void Sign_EncryptedDocument_Throws()
    {
        using var cert = CreateTestCertificate();
        using var pdf = PdfFile.Open(BuildSamplePdf());
        using var encrypted = pdf.Protect(userPassword: "pw");
        using var reopened = PdfFile.Open(encrypted.ToBytes(), "pw");

        Assert.Throws<InvalidOperationException>(() => reopened.Sign(cert));
    }

    [Fact]
    public void SignedDocument_StillOpensAndRenders()
    {
        using var cert = CreateTestCertificate();
        using var pdf = PdfFile.Open(BuildSamplePdf());
        var signed = pdf.Sign(cert);

        using var reopened = PdfFile.Open(signed.ToBytes());
        Assert.Equal(1, reopened.PageCount);
        Assert.Contains("Signed document", reopened.ExtractText());
        Assert.True(reopened.RenderPage(0).Length > 0);
    }

    [Fact]
    public void GetSignatures_UnsignedDocument_ReturnsEmpty()
    {
        using var pdf = PdfFile.Open(BuildSamplePdf());
        Assert.Empty(pdf.GetSignatures());
    }
}
