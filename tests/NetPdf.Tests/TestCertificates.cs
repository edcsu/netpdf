using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;

namespace NetPdf.Tests;

/// <summary>Creates throwaway signing certificates for tests.</summary>
internal static class TestCertificates
{
    /// <summary>
    /// Builds a self-signed RSA-2048 certificate entirely in memory: the public
    /// certificate is created via <c>CertificateRequest.Create</c> and paired with the
    /// key through a hand-built PKCS#12, avoiding <c>CreateSelfSigned</c>'s
    /// <c>CopyWithPrivateKey</c>, which hits the macOS keychain and is flaky on net8.
    /// </summary>
    internal static X509Certificate2 CreateEphemeral(string subject)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(subject, rsa,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var serial = new byte[8];
        RandomNumberGenerator.Fill(serial);
        serial[0] &= 0x7F;
        using var publicCert = request.Create(request.SubjectName,
            X509SignatureGenerator.CreateForRSA(rsa, RSASignaturePadding.Pkcs1),
            DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(1), serial);

        var contents = new Pkcs12SafeContents();
        contents.AddCertificate(publicCert);
        contents.AddKeyUnencrypted(rsa);
        var builder = new Pkcs12Builder();
        builder.AddSafeContentsUnencrypted(contents);
        builder.SealWithMac("", HashAlgorithmName.SHA256, 1);
        var pfx = builder.Encode();

#if NET9_0_OR_GREATER
        return X509CertificateLoader.LoadPkcs12(pfx, "", X509KeyStorageFlags.Exportable);
#else
        return new X509Certificate2(pfx, "", X509KeyStorageFlags.Exportable);
#endif
    }
}
