using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using NetPdf.Reading;

namespace NetPdf.Manipulation;

/// <summary>
/// Writes an XMP metadata packet into a PDF as an incremental update. PDFsharp regenerates
/// the /Metadata stream from the Info dictionary on every save, so the packet is appended
/// as a new revision after the last PDFsharp save instead of being set through PDFsharp.
/// </summary>
internal static partial class XmpWriter
{
    [GeneratedRegex(@"/Metadata\s+\d+\s+0\s+R")]
    private static partial Regex MetadataRefRegex();

    internal static byte[] SetXmp(byte[] pdf, string packet)
    {
        var update = new IncrementalUpdate(pdf);
        var trailer = update.Trailer;
        var newObjNum = update.NextObjectNumber;

        var catDict = IncrementalUpdate.FindObjectBody(trailer, trailer.RootNumber);
        catDict = MetadataRefRegex().IsMatch(catDict)
            ? MetadataRefRegex().Replace(catDict, $"/Metadata {newObjNum} 0 R")
            : catDict.Insert(2, $" /Metadata {newObjNum} 0 R ");

        update.AddStreamObject(newObjNum, "/Type /Metadata /Subtype /XML",
            Encoding.UTF8.GetBytes(packet));
        update.AddObject(trailer.RootNumber, catDict);
        return update.Complete();
    }

    internal static string GeneratePacket(PdfMetadata metadata, bool pdfAIdentification = false)
    {
        var sb = new StringBuilder();
        sb.Append("<?xpacket begin=\"﻿\" id=\"W5M0MpCehiHzreSzNTczkc9d\"?>");
        sb.Append("<x:xmpmeta xmlns:x=\"adobe:ns:meta/\">");
        sb.Append("<rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">");
        sb.Append("<rdf:Description rdf:about=\"\"");
        sb.Append(" xmlns:dc=\"http://purl.org/dc/elements/1.1/\"");
        sb.Append(" xmlns:pdf=\"http://ns.adobe.com/pdf/1.3/\"");
        if (pdfAIdentification)
            sb.Append(" xmlns:pdfaid=\"http://www.aiim.org/pdfa/ns/id/\"");
        sb.Append('>');
        if (pdfAIdentification)
            sb.Append("<pdfaid:part>2</pdfaid:part><pdfaid:conformance>B</pdfaid:conformance>");

        if (metadata.Title is { } title)
            sb.Append($"<dc:title><rdf:Alt><rdf:li xml:lang=\"x-default\">{Escape(title)}</rdf:li></rdf:Alt></dc:title>");
        if (metadata.Author is { } author)
            sb.Append($"<dc:creator><rdf:Seq><rdf:li>{Escape(author)}</rdf:li></rdf:Seq></dc:creator>");
        if (metadata.Subject is { } subject)
            sb.Append($"<dc:description><rdf:Alt><rdf:li xml:lang=\"x-default\">{Escape(subject)}</rdf:li></rdf:Alt></dc:description>");
        if (metadata.Keywords is { } keywords)
            sb.Append($"<pdf:Keywords>{Escape(keywords)}</pdf:Keywords>");
        if (metadata.Producer is { } producer)
            sb.Append($"<pdf:Producer>{Escape(producer)}</pdf:Producer>");

        sb.Append("</rdf:Description></rdf:RDF></x:xmpmeta>");
        sb.Append("<?xpacket end=\"w\"?>");
        return sb.ToString();
    }

    private static string Escape(string value) => SecurityElement.Escape(value);
}
