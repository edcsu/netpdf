using PdfSharp.Fonts;

namespace NetPdf.Creation;

/// <summary>
/// Font resolver that locates TrueType/OpenType fonts in the operating system's
/// standard font directories. PDFsharp requires a resolver on macOS and Linux.
/// </summary>
internal sealed class SystemFontResolver : IFontResolver
{
    private static readonly Lazy<Dictionary<string, string>> FontFiles = new(ScanFontDirectories);

    private static readonly string[] FontDirectories =
    [
        "/System/Library/Fonts/Supplemental",
        "/System/Library/Fonts",
        "/Library/Fonts",
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library/Fonts"),
        "/usr/share/fonts",
        "/usr/local/share/fonts",
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".fonts"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts"),
    ];

    private static readonly string[] FallbackFamilies = ["Arial", "Helvetica", "Liberation Sans", "DejaVu Sans", "Verdana"];

    internal static void Register()
    {
        GlobalFontSettings.FontResolver ??= new SystemFontResolver();
    }

    public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic)
    {
        var face = FindFace(familyName, bold, italic);
        if (face is null)
        {
            foreach (var fallback in FallbackFamilies)
            {
                face = FindFace(fallback, bold, italic);
                if (face is not null)
                    break;
            }
        }

        return face is null ? null : new FontResolverInfo(face);
    }

    public byte[]? GetFont(string faceName) =>
        FontFiles.Value.TryGetValue(faceName, out var path) ? File.ReadAllBytes(path) : null;

    private static string? FindFace(string familyName, bool bold, bool italic)
    {
        var files = FontFiles.Value;
        var family = NormalizeAlnum(familyName);
        var style = StyleToken(bold, italic);

        var key = $"{family}|{style}";
        if (files.ContainsKey(key))
            return key;

        // Fall back to the regular face of the family; PDFsharp can simulate styles.
        key = $"{family}|regular";
        return files.ContainsKey(key) ? key : null;
    }

    private static string StyleToken(bool bold, bool italic) => (bold, italic) switch
    {
        (true, true) => "bolditalic",
        (true, false) => "bold",
        (false, true) => "italic",
        _ => "regular",
    };

    // Strips every non-alphanumeric character so family names match regardless of
    // whether the source (requested family vs. on-disk filename) uses spaces,
    // hyphens, or no separator at all (e.g. "Liberation Sans" vs "LiberationSans-Regular").
    private static string NormalizeAlnum(string name) =>
        new(name.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static Dictionary<string, string> ScanFontDirectories()
    {
        var map = new Dictionary<string, string>();
        foreach (var dir in FontDirectories)
        {
            if (!Directory.Exists(dir))
                continue;

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories);
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var file in files)
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext is not (".ttf" or ".otf"))
                    continue;

                var (family, style) = SplitFamilyAndStyle(Path.GetFileNameWithoutExtension(file));
                map.TryAdd($"{family}|{style}", file);
            }
        }

        return map;
    }

    private static (string Family, string Style) SplitFamilyAndStyle(string baseName)
    {
        var normalized = NormalizeAlnum(baseName);

        // Longest/most specific suffixes first so e.g. "bolditalic" isn't matched as "italic".
        string[] boldItalicSuffixes = ["bolditalic", "boldoblique", "italicbold"];
        string[] boldSuffixes = ["bold"];
        string[] italicSuffixes = ["italic", "oblique"];

        if (TryStripSuffix(normalized, boldItalicSuffixes, out var family))
            return (family, "bolditalic");
        if (TryStripSuffix(normalized, boldSuffixes, out family))
            return (family, "bold");
        if (TryStripSuffix(normalized, italicSuffixes, out family))
            return (family, "italic");
        if (TryStripSuffix(normalized, ["regular"], out family))
            return (family, "regular");

        return (normalized, "regular");
    }

    private static bool TryStripSuffix(string normalized, string[] suffixes, out string family)
    {
        foreach (var suffix in suffixes)
        {
            if (normalized.Length > suffix.Length && normalized.EndsWith(suffix, StringComparison.Ordinal))
            {
                family = normalized[..^suffix.Length];
                return true;
            }
        }

        family = normalized;
        return false;
    }
}
