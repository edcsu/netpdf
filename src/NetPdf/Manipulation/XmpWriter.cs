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
    [GeneratedRegex(@"startxref\s+(\d+)\s+%%EOF\s*$", RegexOptions.Singleline)]
    private static partial Regex StartXrefRegex();

    [GeneratedRegex(@"/Root\s+(\d+)\s+0\s+R")]
    private static partial Regex RootRegex();

    [GeneratedRegex(@"/Size\s+(\d+)")]
    private static partial Regex SizeRegex();

    [GeneratedRegex(@"/ID\s*(\[[^\]]*\])")]
    private static partial Regex IdRegex();

    [GeneratedRegex(@"/Metadata\s+\d+\s+0\s+R")]
    private static partial Regex MetadataRefRegex();

    internal static byte[] SetXmp(byte[] pdf, string packet)
    {
        // Latin-1 is byte-transparent, keeping string indexes equal to byte offsets.
        var text = Encoding.Latin1.GetString(pdf);

        var startxref = StartXrefRegex().Match(text);
        var root = RootRegex().Match(text);
        var size = SizeRegex().Match(text);
        if (!startxref.Success || !root.Success || !size.Success)
            throw new InvalidOperationException("Unsupported PDF structure: file trailer not found.");
        var prevXref = long.Parse(startxref.Groups[1].Value);
        var rootNum = int.Parse(root.Groups[1].Value);
        var newObjNum = int.Parse(size.Groups[1].Value);

        var catalog = Regex.Match(text, $@"(?<!\d){rootNum}\s+0\s+obj\s*(<<.*?>>)\s*endobj",
            RegexOptions.Singleline);
        if (!catalog.Success)
            throw new InvalidOperationException("Unsupported PDF structure: document catalog not found.");
        var catDict = catalog.Groups[1].Value;
        catDict = MetadataRefRegex().IsMatch(catDict)
            ? MetadataRefRegex().Replace(catDict, $"/Metadata {newObjNum} 0 R")
            : catDict.Insert(2, $" /Metadata {newObjNum} 0 R ");

        var xmpBytes = Encoding.UTF8.GetBytes(packet);
        using var ms = new MemoryStream();
        ms.Write(pdf);
        void Write(string s) => ms.Write(Encoding.Latin1.GetBytes(s));
        if (pdf[^1] != (byte)'\n')
            Write("\n");
        var metaOffset = ms.Position;
        Write($"{newObjNum} 0 obj\n<< /Type /Metadata /Subtype /XML /Length {xmpBytes.Length} >>\nstream\n");
        ms.Write(xmpBytes);
        Write("\nendstream\nendobj\n");
        var catOffset = ms.Position;
        Write($"{rootNum} 0 obj\n{catDict}\nendobj\n");
        var xrefOffset = ms.Position;
        Write("xref\n0 1\n0000000000 65535 f \n");
        Write($"{rootNum} 1\n{catOffset:0000000000} 00000 n \n");
        Write($"{newObjNum} 1\n{metaOffset:0000000000} 00000 n \n");
        Write($"trailer\n<< /Size {newObjNum + 1} /Root {rootNum} 0 R /Prev {prevXref}");
        var id = IdRegex().Match(text);
        if (id.Success)
            Write($" /ID {id.Groups[1].Value}");
        Write($" >>\nstartxref\n{xrefOffset}\n%%EOF\n");
        return ms.ToArray();
    }

    internal static string GeneratePacket(PdfMetadata metadata)
    {
        var sb = new StringBuilder();
        sb.Append("<?xpacket begin=\"﻿\" id=\"W5M0MpCehiHzreSzNTczkc9d\"?>");
        sb.Append("<x:xmpmeta xmlns:x=\"adobe:ns:meta/\">");
        sb.Append("<rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">");
        sb.Append("<rdf:Description rdf:about=\"\"");
        sb.Append(" xmlns:dc=\"http://purl.org/dc/elements/1.1/\"");
        sb.Append(" xmlns:pdf=\"http://ns.adobe.com/pdf/1.3/\">");

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
