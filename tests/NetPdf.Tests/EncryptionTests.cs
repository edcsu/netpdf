using Xunit;

namespace NetPdf.Tests;

public class EncryptionTests
{
    private static byte[] CreateSample(string text = "Secret content") =>
        PdfFile.Create().AddPage(p => p.AddText(text, 50, 50)).ToBytes();

    [Fact]
    public void Aes256_protected_document_requires_password()
    {
        using var doc = PdfFile.Open(CreateSample());
        using var locked = doc.Protect(userPassword: "pw");
        var bytes = locked.ToBytes();

        Assert.ThrowsAny<Exception>(() =>
        {
            using var noPw = PdfFile.Open(bytes);
            _ = noPw.PageCount;
        });
    }

    [Fact]
    public void Aes256_protected_document_opens_and_extracts_with_password()
    {
        // Gate for the AES-256 default: PdfPig must be able to read our AES-256 output.
        using var doc = PdfFile.Open(CreateSample("AES round trip"));
        var bytes = doc.Protect(userPassword: "pw").ToBytes();

        using var reopened = PdfFile.Open(bytes, "pw");
        Assert.Equal(1, reopened.PageCount);
        Assert.Contains("AES round trip", reopened.ExtractText());
    }

    [Fact]
    public void Rc4_protection_still_works()
    {
        using var doc = PdfFile.Open(CreateSample("RC4 round trip"));
        var bytes = doc.Protect(userPassword: "pw", algorithm: EncryptionAlgorithm.Rc4_128).ToBytes();

        using var reopened = PdfFile.Open(bytes, "pw");
        Assert.Contains("RC4 round trip", reopened.ExtractText());
    }

    [Fact]
    public void Wrong_password_throws()
    {
        using var doc = PdfFile.Open(CreateSample());
        var bytes = doc.Protect(userPassword: "right").ToBytes();

        Assert.ThrowsAny<Exception>(() =>
        {
            using var wrong = PdfFile.Open(bytes, "wrong");
            _ = wrong.PageCount;
        });
    }

    [Fact]
    public void Decrypt_removes_password_requirement()
    {
        using var doc = PdfFile.Open(CreateSample("Decrypt me"));
        var lockedBytes = doc.Protect(userPassword: "pw").ToBytes();

        using var locked = PdfFile.Open(lockedBytes, "pw");
        using var unlocked = locked.Decrypt();

        using var reopened = PdfFile.Open(unlocked.ToBytes());
        Assert.Contains("Decrypt me", reopened.ExtractText());
    }

    [Fact]
    public void Protected_document_can_still_be_manipulated()
    {
        var bytes = PdfFile.Create()
            .AddPage(p => p.AddText("page 1", 50, 50))
            .AddPage(p => p.AddText("page 2", 50, 50))
            .ToBytes();
        using var doc = PdfFile.Open(bytes);
        var lockedBytes = doc.Protect(userPassword: "pw").ToBytes();

        using var locked = PdfFile.Open(lockedBytes, "pw");
        using var single = locked.ExtractPages(1);

        Assert.Equal(1, single.PageCount);
        Assert.Contains("page 2", single.ExtractText());
    }
}
