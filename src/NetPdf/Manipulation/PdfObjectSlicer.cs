using System.Text;
using System.Text.RegularExpressions;

namespace NetPdf.Manipulation;

/// <summary>One indirect object located in a PDF file, as a raw byte slice.</summary>
internal sealed class SlicedObject
{
    /// <summary>The object number.</summary>
    public required int Number { get; init; }

    /// <summary>The full object bytes from <c>n 0 obj</c> through <c>endobj</c>.</summary>
    public required byte[] Bytes { get; init; }

    /// <summary>
    /// The object body split at the stream payload: everything before <c>stream</c> (where
    /// indirect references can appear) and the remainder (payload + <c>endstream endobj</c>).
    /// <c>Tail</c> is empty for non-stream objects.
    /// </summary>
    public required string Head { get; init; }

    /// <summary>Raw bytes from the <c>stream</c> keyword onward; empty for non-stream objects.</summary>
    public required byte[] Tail { get; init; }

    /// <summary>Object numbers this object references via <c>n 0 R</c> in its dictionary part.</summary>
    public required IReadOnlyList<int> References { get; init; }
}

/// <summary>
/// Parses a classic-xref PDF into individually addressable object byte slices, following
/// the /Prev chain across incremental updates (newest revision wins per object number).
/// Cross-reference streams and object streams are not supported.
/// </summary>
internal static partial class PdfObjectSlicer
{
    [GeneratedRegex(@"(\d+)\s+0\s+R")]
    private static partial Regex ReferenceRegex();

    [GeneratedRegex(@"/Prev\s+(\d+)")]
    private static partial Regex PrevRegex();

    /// <summary>Reads all in-use objects of <paramref name="pdf"/>.</summary>
    internal static Dictionary<int, SlicedObject> Slice(byte[] pdf)
    {
        var text = Encoding.Latin1.GetString(pdf);
        var (offsets, allOffsets) = ReadXrefChain(text);

        // Object slices end where the next object (including superseded revisions),
        // an xref section, or the file ends.
        var boundaries = allOffsets
            .Concat(AllIndexesOf(text, "\nxref").Select(i => (long)(i + 1)))
            .Concat(AllIndexesOf(text, "\ntrailer").Select(i => (long)(i + 1)))
            .Append(pdf.Length)
            .Distinct()
            .Order()
            .ToArray();

        var result = new Dictionary<int, SlicedObject>();
        foreach (var (number, offset) in offsets)
        {
            var endIndex = Array.BinarySearch(boundaries, offset);
            var end = boundaries[endIndex + 1];
            var slice = text[(int)offset..(int)end];
            var endobj = slice.LastIndexOf("endobj", StringComparison.Ordinal);
            if (endobj < 0)
                throw new InvalidOperationException($"Object {number} is malformed: endobj not found.");
            slice = slice[..(endobj + "endobj".Length)] + "\n";

            var streamIdx = FindStreamKeyword(slice);
            var head = streamIdx < 0 ? slice : slice[..streamIdx];
            var tail = streamIdx < 0 ? "" : slice[streamIdx..];
            var refs = ReferenceRegex().Matches(head)
                .Select(m => int.Parse(m.Groups[1].Value))
                .Distinct()
                .ToArray();

            result[number] = new SlicedObject
            {
                Number = number,
                Bytes = Encoding.Latin1.GetBytes(slice),
                Head = head,
                Tail = Encoding.Latin1.GetBytes(tail),
                References = refs,
            };
        }
        return result;
    }

    /// <summary>
    /// Finds the <c>stream</c> keyword that starts a stream payload (not e.g. /Metadata
    /// values); it directly follows the dictionary's closing <c>&gt;&gt;</c>.
    /// </summary>
    private static int FindStreamKeyword(string slice)
    {
        var m = Regex.Match(slice, @">>\s*stream(\r\n|\n)");
        return m.Success ? m.Index + slice[m.Index..].IndexOf("stream", StringComparison.Ordinal) : -1;
    }

    /// <summary>Walks the xref chain from the last startxref; the newest definition of each object wins.</summary>
    private static (Dictionary<int, long> Offsets, List<long> AllOffsets) ReadXrefChain(string text)
    {
        var startxref = Regex.Match(text, @"startxref\s+(\d+)\s+%%EOF\s*$", RegexOptions.Singleline);
        if (!startxref.Success)
            throw new InvalidOperationException("Unsupported PDF structure: startxref not found.");

        var offsets = new Dictionary<int, long>();
        var allOffsets = new List<long>();
        long? next = long.Parse(startxref.Groups[1].Value);
        var visited = new HashSet<long>();
        while (next is { } xrefPos && visited.Add(xrefPos))
        {
            if (xrefPos >= text.Length || !text[(int)xrefPos..].StartsWith("xref", StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Unsupported PDF structure: cross-reference streams are not supported.");
            var section = text[(int)xrefPos..];
            var trailerIdx = section.IndexOf("trailer", StringComparison.Ordinal);
            var table = section[4..trailerIdx];

            var pos = 0;
            var lines = table.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            while (pos < lines.Length)
            {
                var header = lines[pos].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var first = int.Parse(header[0]);
                var count = int.Parse(header[1]);
                for (var i = 0; i < count; i++)
                {
                    var entry = lines[pos + 1 + i];
                    if (entry[17] == 'n')
                    {
                        var number = first + i;
                        var offset = long.Parse(entry[..10]);
                        allOffsets.Add(offset);
                        // Newest-first walk: keep the first (most recent) definition.
                        offsets.TryAdd(number, offset);
                    }
                }
                pos += 1 + count;
            }

            var trailer = section[trailerIdx..(trailerIdx + Math.Min(2048, section.Length - trailerIdx))];
            var prev = PrevRegex().Match(trailer);
            next = prev.Success ? long.Parse(prev.Groups[1].Value) : null;
        }
        offsets.Remove(0);
        return (offsets, allOffsets);
    }

    private static IEnumerable<int> AllIndexesOf(string text, string needle)
    {
        for (var i = text.IndexOf(needle, StringComparison.Ordinal); i >= 0;
             i = text.IndexOf(needle, i + 1, StringComparison.Ordinal))
            yield return i;
    }
}
