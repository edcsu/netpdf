using System.Text;
using System.Text.RegularExpressions;

namespace NetPdf.Manipulation;

/// <summary>
/// Parsed facts about a PDF's last trailer, needed to append an incremental update.
/// </summary>
internal sealed class TrailerInfo
{
    /// <summary>Byte offset of the previous cross-reference section (the last startxref value).</summary>
    public required long PrevXref { get; init; }

    /// <summary>Object number of the document catalog (/Root).</summary>
    public required int RootNumber { get; init; }

    /// <summary>The trailer's /Size — the next free object number.</summary>
    public required int Size { get; init; }

    /// <summary>The raw /ID array including brackets, or null when absent.</summary>
    public string? Id { get; init; }

    /// <summary>The whole file decoded as Latin-1, so string indexes equal byte offsets.</summary>
    public required string Text { get; init; }
}

/// <summary>
/// Appends objects to an existing PDF as an incremental update: new and replacement objects
/// followed by a classic cross-reference section whose trailer chains to the previous one
/// via /Prev. Callers add objects, then call <see cref="Complete"/> once.
/// </summary>
internal sealed partial class IncrementalUpdate
{
    [GeneratedRegex(@"startxref\s+(\d+)\s+%%EOF\s*$", RegexOptions.Singleline)]
    private static partial Regex StartXrefRegex();

    [GeneratedRegex(@"/Root\s+(\d+)\s+0\s+R")]
    private static partial Regex RootRegex();

    [GeneratedRegex(@"/Size\s+(\d+)")]
    private static partial Regex SizeRegex();

    [GeneratedRegex(@"/ID\s*(\[[^\]]*\])")]
    private static partial Regex IdRegex();

    private readonly TrailerInfo _trailer;
    private readonly MemoryStream _output = new();
    private readonly List<(int Number, long Offset)> _objects = [];

    /// <summary>Parses the trailer of <paramref name="pdf"/>; throws when the structure is unsupported.</summary>
    internal static TrailerInfo ParseTrailer(byte[] pdf)
    {
        // Latin-1 is byte-transparent, keeping string indexes equal to byte offsets.
        var text = Encoding.Latin1.GetString(pdf);
        var startxref = StartXrefRegex().Match(text);
        var root = RootRegex().Match(text);
        var size = SizeRegex().Match(text);
        if (!startxref.Success || !root.Success || !size.Success)
            throw new InvalidOperationException("Unsupported PDF structure: file trailer not found.");
        var id = IdRegex().Match(text);
        return new TrailerInfo
        {
            PrevXref = long.Parse(startxref.Groups[1].Value),
            RootNumber = int.Parse(root.Groups[1].Value),
            Size = int.Parse(size.Groups[1].Value),
            Id = id.Success ? id.Groups[1].Value : null,
            Text = text,
        };
    }

    /// <summary>
    /// Finds the body (dictionary) of object <paramref name="objectNumber"/> in the current
    /// revision, e.g. the catalog dictionary. Returns the text between <c>obj</c> and <c>endobj</c>.
    /// </summary>
    internal static string FindObjectBody(TrailerInfo trailer, int objectNumber)
    {
        var match = Regex.Match(trailer.Text, $@"(?<!\d){objectNumber}\s+0\s+obj\s*(<<.*?>>)\s*endobj",
            RegexOptions.Singleline);
        if (!match.Success)
            throw new InvalidOperationException($"Unsupported PDF structure: object {objectNumber} not found.");
        return match.Groups[1].Value;
    }

    internal IncrementalUpdate(byte[] pdf)
    {
        _trailer = ParseTrailer(pdf);
        _output.Write(pdf);
        if (pdf[^1] != (byte)'\n')
            WriteRaw("\n");
    }

    /// <summary>The parsed trailer of the original file.</summary>
    internal TrailerInfo Trailer => _trailer;

    /// <summary>The next free object number in the original file.</summary>
    internal int NextObjectNumber => _trailer.Size;

    /// <summary>Current byte offset in the output (where the next write lands).</summary>
    internal long Position => _output.Position;

    /// <summary>Appends an object whose content is plain text (a dictionary or other value).</summary>
    internal void AddObject(int number, string content)
    {
        _objects.Add((number, _output.Position));
        WriteRaw($"{number} 0 obj\n{content}\nendobj\n");
    }

    /// <summary>Appends a stream object with the given dictionary entries (without /Length) and payload.</summary>
    internal void AddStreamObject(int number, string dictionaryEntries, byte[] payload)
    {
        _objects.Add((number, _output.Position));
        WriteRaw($"{number} 0 obj\n<< {dictionaryEntries} /Length {payload.Length} >>\nstream\n");
        _output.Write(payload);
        WriteRaw("\nendstream\nendobj\n");
    }

    /// <summary>
    /// Writes the cross-reference section and trailer and returns the finished file.
    /// <paramref name="extraTrailerEntries"/> may add entries such as <c>/Encrypt</c>.
    /// </summary>
    internal byte[] Complete(string? extraTrailerEntries = null)
    {
        var xrefOffset = _output.Position;
        WriteRaw("xref\n0 1\n0000000000 65535 f \n");
        foreach (var (number, offset) in _objects.OrderBy(o => o.Number))
            WriteRaw($"{number} 1\n{offset:0000000000} 00000 n \n");
        var newSize = Math.Max(_trailer.Size, _objects.Max(o => o.Number) + 1);
        WriteRaw($"trailer\n<< /Size {newSize} /Root {_trailer.RootNumber} 0 R /Prev {_trailer.PrevXref}");
        if (_trailer.Id is { } id)
            WriteRaw($" /ID {id}");
        if (extraTrailerEntries is { } extra)
            WriteRaw($" {extra}");
        WriteRaw($" >>\nstartxref\n{xrefOffset}\n%%EOF\n");
        return _output.ToArray();
    }

    private void WriteRaw(string s) => _output.Write(Encoding.Latin1.GetBytes(s));
}
