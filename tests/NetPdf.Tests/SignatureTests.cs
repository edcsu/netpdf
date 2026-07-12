using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using NetPdf.Fluent;
using Xunit;

namespace NetPdf.Tests;

public class SignatureTests
{
    private static X509Certificate2 CreateTestCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=NetPdf Test Signer", rsa,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(1));
    }

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
