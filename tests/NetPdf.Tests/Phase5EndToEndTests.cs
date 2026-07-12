using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using NetPdf.Fluent;
using Xunit;

namespace NetPdf.Tests;

/// <summary>
/// Exercises the full Phase 5 pipeline in the documented order:
/// build (tagged + PDF/A + debug overlays + RTL) → Linearize → Sign → verify.
/// </summary>
public class Phase5EndToEndTests
{
    [Fact]
    public void FullPipeline_BuildLinearizeSignVerify()
    {
        var qr = NetPdf.Creation.BarcodeGenerator.GenerateQrCode("e2e", 64);
        var built = Document.Create(doc => doc
                .Page(page => page
                    .DebugOverlay()
                    .Header(h => h.Debug("hdr").Text("Report"))
                    .Content(c => c.Column(col =>
                    {
                        col.Item().Heading(1).Text("Quarterly Summary");
                        col.Item().Paragraph().Text("All systems nominal.");
                        col.Item().Width(60).Image(qr, altText: "QR code");
                        col.Item().ContentFromRightToLeft().Text("שלום עולם");
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(cols => { cols.RelativeColumn(); cols.RelativeColumn(); });
                            t.Cell().Text("Metric");
                            t.Cell().Text("Value");
                        });
                    }))
                    .Footer(f => f.PageNumber("{number}/{total}")))
                .Page(page => page.Content(c => c.Text("Appendix"))))
            .WithTagging()
            .AsPdfA()
            .ToBytes();

        using var cert = TestCertificates.CreateEphemeral("CN=E2E Signer");

        using var doc = PdfFile.Open(built);
        using var linearized = doc.Linearize();
        using var signed = linearized.Sign(cert);

        using var final = PdfFile.Open(signed.ToBytes());
        Assert.Equal(2, final.PageCount);
        Assert.Contains("Quarterly Summary", final.ExtractText());
        Assert.Contains("Appendix", final.ExtractText(1));

        var sig = Assert.Single(final.GetSignatures());
        Assert.True(sig.IsIntact);
        Assert.True(sig.CoversWholeDocument);

        foreach (var png in final.RenderAllPages(dpi: 96))
            Assert.True(png.Length > 0);
    }
}
