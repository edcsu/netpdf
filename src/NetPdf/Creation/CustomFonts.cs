namespace NetPdf.Creation;

/// <summary>
/// Fonts registered by the app through <see cref="NetPdfFonts"/>.
/// </summary>
internal static class CustomFonts
{
    private static readonly Dictionary<string, byte[]> Faces = new();

    internal static void Add(string family, bool bold, bool italic, byte[] data)
    {
        lock (Faces)
            Faces[Key(family, bold, italic)] = data;
    }

    // Exact style match first, then the regular face of the same family.
    internal static string? Find(string family, bool bold, bool italic)
    {
        lock (Faces)
        {
            var key = Key(family, bold, italic);
            if (Faces.ContainsKey(key))
                return key;
            key = Key(family, false, false);
            return Faces.ContainsKey(key) ? key : null;
        }
    }

    internal static bool TryGetBytes(string faceName, out byte[]? bytes)
    {
        lock (Faces)
            return Faces.TryGetValue(faceName, out bytes);
    }

    private static string Key(string family, bool bold, bool italic) =>
        $"@custom|{SystemFontResolver.NormalizeAlnum(family)}|{SystemFontResolver.StyleToken(bold, italic)}";
}
