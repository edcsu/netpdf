using Xunit;

namespace NetPdf.Tests;

public class XmpTests
{
    private const string SamplePacket =
        "<?xpacket begin=\"﻿\" id=\"W5M0MpCehiHzreSzNTczkc9d\"?>" +
        "<x:xmpmeta xmlns:x=\"adobe:ns:meta/\">" +
        "<rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">" +
        "<rdf:Description rdf:about=\"\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\">" +
        "<dc:title><rdf:Alt><rdf:li xml:lang=\"x-default\">Custom XMP Title</rdf:li></rdf:Alt></dc:title>" +
        "</rdf:Description></rdf:RDF></x:xmpmeta>" +
        "<?xpacket end=\"w\"?>";

    private static byte[] CreateSample() =>
        PdfFile.Create()
            .WithMetadata(m => m.Title("Info Title").Author("Info Author"))
            .AddPage(p => p.AddText("XMP host", 50, 50))
            .ToBytes();

    [Fact]
    public void Raw_xmp_packet_round_trips()
    {
        using var doc = PdfFile.Open(CreateSample());
        using var withXmp = doc.WithXmpMetadata(SamplePacket);

        using var reopened = PdfFile.Open(withXmp.ToBytes());
        var xmp = reopened.GetXmpMetadata();
        Assert.NotNull(xmp);
        Assert.Contains("Custom XMP Title", xmp);
    }

    [Fact]
    public void Generated_xmp_reflects_info_metadata()
    {
        using var doc = PdfFile.Open(CreateSample());
        using var withXmp = doc.WithGeneratedXmpMetadata();

        using var reopened = PdfFile.Open(withXmp.ToBytes());
        var xmp = reopened.GetXmpMetadata();
        Assert.NotNull(xmp);
        Assert.Contains("Info Title", xmp);
        Assert.Contains("Info Author", xmp);
    }

    [Fact]
    public void Document_content_survives_xmp_write()
    {
        using var doc = PdfFile.Open(CreateSample());
        using var withXmp = doc.WithXmpMetadata(SamplePacket);

        using var reopened = PdfFile.Open(withXmp.ToBytes());
        Assert.Equal(1, reopened.PageCount);
        Assert.Contains("XMP host", reopened.ExtractText());
        Assert.Equal("Info Title", reopened.Metadata.Title);
    }

    [Fact]
    public void Xmp_survives_pdfsharp_reopen_but_not_resave()
    {
        // A later PDFsharp-based manipulation regenerates XMP from Info — documented behavior.
        using var doc = PdfFile.Open(CreateSample());
        using var withXmp = doc.WithXmpMetadata(SamplePacket);
        using var rotated = withXmp.RotatePage(0, Rotation.Rotate180);

        Assert.Equal(1, rotated.PageCount); // file stays structurally valid
        var xmp = rotated.GetXmpMetadata();
        Assert.True(xmp is null || !xmp.Contains("Custom XMP Title"));
    }

    [Fact]
    public void Generated_xmp_escapes_xml_characters()
    {
        var bytes = PdfFile.Create()
            .WithMetadata(m => m.Title("A <B> & C"))
            .AddPage(p => p.AddText("x", 0, 0))
            .ToBytes();
        using var doc = PdfFile.Open(bytes);
        using var withXmp = doc.WithGeneratedXmpMetadata();

        using var reopened = PdfFile.Open(withXmp.ToBytes());
        Assert.Contains("A &lt;B&gt; &amp; C", reopened.GetXmpMetadata());
    }

    [Fact]
    public void Encrypted_document_rejects_xmp_write()
    {
        using var doc = PdfFile.Open(CreateSample());
        var lockedBytes = doc.Protect(userPassword: "pw").ToBytes();
        using var locked = PdfFile.Open(lockedBytes, "pw");

        Assert.Throws<InvalidOperationException>(() => locked.WithXmpMetadata(SamplePacket));
    }
}
