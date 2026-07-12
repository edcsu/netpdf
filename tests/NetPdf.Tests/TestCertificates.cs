using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace NetPdf.Tests;

/// <summary>Creates throwaway signing certificates for tests.</summary>
internal static class TestCertificates
{
    /// <summary>
    /// Builds a self-signed RSA-2048 certificate and round-trips it through PKCS#12 so
    /// the private key is fully in-memory (avoids macOS keychain flakiness on net8).
    /// </summary>
    internal static X509Certificate2 CreateEphemeral(string subject)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(subject, rsa,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var cert = request.CreateSelfSigned(
            DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(1));
        var pfx = cert.Export(X509ContentType.Pkcs12);
#if NET9_0_OR_GREATER
        return X509CertificateLoader.LoadPkcs12(pfx, null, X509KeyStorageFlags.Exportable);
#else
        return new X509Certificate2(pfx, (string?)null, X509KeyStorageFlags.Exportable);
#endif
    }
}
