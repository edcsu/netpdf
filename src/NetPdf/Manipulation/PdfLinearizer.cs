using System.Text;
using System.Text.RegularExpressions;

namespace NetPdf.Manipulation;

/// <summary>
/// Rewrites a PDF into linearized ("fast web view") form: a linearization parameter
/// dictionary first, then the first page's cross-reference table, hint stream, catalog,
/// and first-page objects, followed by the remaining objects and the main cross-reference
/// table. Hint tables are structurally valid but simplified — the practical benefit is
/// first-page-first object ordering, which is what modern consumers act on.
/// </summary>
internal static partial class PdfLinearizer
{
    [GeneratedRegex(@"/Pages\s+(\d+)\s+0\s+R")]
    private static partial Regex PagesRefRegex();

    [GeneratedRegex(@"/Kids\s*\[([^\]]*)\]")]
    private static partial Regex KidsRegex();

    [GeneratedRegex(@"(\d+)\s+0\s+R")]
    private static partial Regex ReferenceRegex();

    internal static byte[] Linearize(byte[] pdf)
    {
        var trailer = IncrementalUpdate.ParseTrailer(pdf);
        var objects = PdfObjectSlicer.Slice(pdf);

        // Identify the catalog, page tree root, and page objects.
        var rootNum = trailer.RootNumber;
        var pagesRef = PagesRefRegex().Match(objects[rootNum].Head);
        if (!pagesRef.Success)
            throw new InvalidOperationException("Unsupported PDF structure: /Pages reference not found.");
        var pagesNum = int.Parse(pagesRef.Groups[1].Value);
        var kids = KidsRegex().Match(objects[pagesNum].Head);
        if (!kids.Success)
            throw new InvalidOperationException("Unsupported PDF structure: page tree has no /Kids.");
        var pageNums = ReferenceRegex().Matches(kids.Groups[1].Value)
            .Select(m => int.Parse(m.Groups[1].Value))
            .ToArray();
        var firstPageNum = pageNums[0];
        var otherPages = pageNums.Skip(1).ToHashSet();

        // First-page set: catalog, page tree root, first page, and everything the first
        // page references transitively — without descending into other pages.
        var firstSet = new HashSet<int> { rootNum, pagesNum, firstPageNum };
        var queue = new Queue<int>();
        foreach (var r in objects[firstPageNum].References)
            queue.Enqueue(r);
        foreach (var r in objects[rootNum].References)
            queue.Enqueue(r);
        while (queue.TryDequeue(out var n))
        {
            if (!objects.ContainsKey(n) || otherPages.Contains(n) || n == pagesNum || !firstSet.Add(n))
                continue;
            foreach (var r in objects[n].References)
                queue.Enqueue(r);
        }

        var restNums = objects.Keys.Where(n => !firstSet.Contains(n)).Order().ToArray();
        // New numbering: remaining objects 1..r; linearization dict r+1; hint stream r+2;
        // catalog and first-page objects follow, catalog first (required near the front).
        var map = new Dictionary<int, int>();
        var next = 1;
        foreach (var n in restNums)
            map[n] = next++;
        var linDictNum = next++;
        var hintNum = next++;
        var firstOrdered = new List<int> { rootNum };
        firstOrdered.AddRange(firstSet.Where(n => n != rootNum).Order());
        foreach (var n in firstOrdered)
            map[n] = next++;
        var total = next - 1;
        var pageCount = pageNums.Length;

        string Renumber(SlicedObject obj)
        {
            var head = ReferenceRegex().Replace(obj.Head, m => $"{map[int.Parse(m.Groups[1].Value)]} 0 R");
            // Rewrite the object header "old 0 obj" to the new number.
            return Regex.Replace(head, @"^\s*\d+\s+0\s+obj", $"{map[obj.Number]} 0 obj");
        }

        using var ms = new MemoryStream();
        void Write(string s) => ms.Write(Encoding.Latin1.GetBytes(s));
        var patches = new List<(long Offset, Func<long> Value)>();
        void Placeholder(Func<long> value)
        {
            patches.Add((ms.Position, value));
            Write(new string('0', 10)); // fixed-width numeric placeholder
        }

        Write("%PDF-1.7\n%âãÏÓ\n");

        // Part 2: linearization parameter dictionary.
        long linE = 0, hintOffset = 0, hintLength = 0, mainXrefEntryOffset = 0, firstXrefOffset = 0;
        var offsets = new Dictionary<int, long> { [linDictNum] = ms.Position };
        Write($"{linDictNum} 0 obj\n<< /Linearized 1 /L ");
        Placeholder(() => ms.Length);
        Write(" /H [");
        Placeholder(() => hintOffset);
        Write(" ");
        Placeholder(() => hintLength);
        Write($"] /O {map[firstPageNum]} /E ");
        Placeholder(() => linE);
        Write($" /N {pageCount} /T ");
        Placeholder(() => mainXrefEntryOffset);
        Write(" >>\nendobj\n");

        // Part 3: first-page cross-reference table (covers linDict..total) and trailer.
        firstXrefOffset = ms.Position;
        Write("xref\n");
        Write($"{linDictNum} {total - linDictNum + 1}\n");
        var firstXrefEntries = ms.Position;
        for (var i = linDictNum; i <= total; i++)
            Write("0000000000 00000 n \n");
        long mainXrefOffset = 0;
        Write($"trailer\n<< /Size {total + 1} /Root {map[rootNum]} 0 R /Prev ");
        Placeholder(() => mainXrefOffset);
        if (trailer.Id is { } id)
            Write($" /ID {id}");
        Write(" >>\nstartxref\n0\n%%EOF\n");

        // Part 4/5: hint stream, catalog, first-page objects.
        var hintPayload = BuildHintStream(pageCount);
        hintOffset = ms.Position;
        offsets[hintNum] = ms.Position;
        Write($"{hintNum} 0 obj\n<< /S 0 /Length {hintPayload.Length} >>\nstream\n");
        ms.Write(hintPayload);
        Write("\nendstream\nendobj\n");
        hintLength = ms.Position - hintOffset;

        foreach (var n in firstOrdered)
        {
            offsets[map[n]] = ms.Position;
            var obj = objects[n];
            Write(Renumber(obj));
            ms.Write(obj.Tail);
        }
        linE = ms.Position;

        // Part 7+: remaining objects.
        foreach (var n in restNums)
        {
            offsets[map[n]] = ms.Position;
            var obj = objects[n];
            Write(Renumber(obj));
            ms.Write(obj.Tail);
        }

        // Part 11: main cross-reference table covering 0..r.
        mainXrefOffset = ms.Position;
        Write("xref\n");
        Write($"0 {restNums.Length + 1}\n");
        mainXrefEntryOffset = ms.Position;
        Write("0000000000 65535 f \n");
        for (var i = 1; i <= restNums.Length; i++)
            Write($"{offsets[i]:0000000000} 00000 n \n");
        Write($"trailer\n<< /Size {restNums.Length + 1} >>\nstartxref\n{firstXrefOffset}\n%%EOF\n");

        var bytes = ms.ToArray();

        // Back-patch the first xref entries and the fixed-width placeholders.
        var pos = firstXrefEntries;
        for (var i = linDictNum; i <= total; i++)
        {
            Encoding.Latin1.GetBytes($"{offsets[i]:0000000000}").CopyTo(bytes, pos);
            pos += 20;
        }
        foreach (var (offset, value) in patches)
            Encoding.Latin1.GetBytes($"{value():0000000000}").CopyTo(bytes, offset);

        return bytes;
    }

    /// <summary>
    /// Builds a simplified page-offset hint table: structurally per spec (header of 13
    /// fields followed by per-page items) with generous item widths. Values are nominal;
    /// consumers that honor hints will fall back to normal parsing.
    /// </summary>
    private static byte[] BuildHintStream(int pageCount)
    {
        var bits = new List<bool>();
        void WriteBits(long value, int width)
        {
            for (var i = width - 1; i >= 0; i--)
                bits.Add((value >> i & 1) == 1);
        }

        // Header (Table F.3): least objects per page, first page location, bits per
        // object count, least page length, bits per length, least content offset, bits
        // per offset, least content length, bits per content length, bits per shared
        // object count, bits per shared identifier, bits per fraction numerator,
        // denominator.
        WriteBits(1, 32);
        WriteBits(0, 32);
        WriteBits(16, 16);
        WriteBits(0, 32);
        WriteBits(32, 16);
        WriteBits(0, 32);
        WriteBits(32, 16);
        WriteBits(0, 32);
        WriteBits(32, 16);
        WriteBits(0, 16);
        WriteBits(0, 16);
        WriteBits(0, 16);
        WriteBits(1, 16);

        // Per-page items: object count delta, page length, shared object count,
        // content offset, content length.
        for (var p = 0; p < pageCount; p++)
            WriteBits(0, 16);
        for (var p = 0; p < pageCount; p++)
            WriteBits(0, 32);
        for (var p = 0; p < pageCount; p++)
            WriteBits(0, 32);
        for (var p = 0; p < pageCount; p++)
            WriteBits(0, 32);

        var bytes = new byte[(bits.Count + 7) / 8];
        for (var i = 0; i < bits.Count; i++)
            if (bits[i])
                bytes[i / 8] |= (byte)(1 << (7 - i % 8));
        return bytes;
    }
}
