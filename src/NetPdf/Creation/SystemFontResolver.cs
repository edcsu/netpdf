using PdfSharp.Fonts;

namespace NetPdf.Creation;

/// <summary>
/// Font resolver that locates TrueType/OpenType fonts in the operating system's
/// standard font directories. PDFsharp requires a resolver on macOS and Linux.
/// Order: fonts you registered, then OS fonts, then the bundled Liberation Sans
/// (so a container with no fonts still works).
/// </summary>
internal sealed class SystemFontResolver : IFontResolver
{
    private const string EmbeddedPrefix = "@embedded|";

    private readonly Lazy<Dictionary<string, string>> _fontFiles;

    private static readonly string[] DefaultFontDirectories =
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

    internal SystemFontResolver() : this(DefaultFontDirectories)
    {
    }

    // Tests pass their own folders (or none) to simulate a machine with no fonts.
    internal SystemFontResolver(IEnumerable<string> fontDirectories)
    {
        var dirs = fontDirectories.ToArray();
        _fontFiles = new Lazy<Dictionary<string, string>>(() => ScanFontDirectories(dirs));
    }

    internal static void Register()
    {
        GlobalFontSettings.FontResolver ??= new SystemFontResolver();
    }

    public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic)
    {
        // 1. Fonts the user registered.
        var custom = CustomFonts.Find(familyName, bold, italic);
        if (custom is not null)
            return new FontResolverInfo(custom);

        // 2. OS fonts, then the old fallback list.
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

        // 3. Bundled Liberation Sans. Never returns null.
        face ??= EmbeddedPrefix + StyleToken(bold, italic);
        return new FontResolverInfo(face);
    }

    public byte[]? GetFont(string faceName)
    {
        if (CustomFonts.TryGetBytes(faceName, out var bytes))
            return bytes;
        if (faceName.StartsWith(EmbeddedPrefix, StringComparison.Ordinal))
            return LoadEmbedded(faceName[EmbeddedPrefix.Length..]);
        return _fontFiles.Value.TryGetValue(faceName, out var path) ? File.ReadAllBytes(path) : null;
    }

    private static readonly Dictionary<string, byte[]> EmbeddedCache = new();

    private static byte[]? LoadEmbedded(string style)
    {
        lock (EmbeddedCache)
        {
            if (EmbeddedCache.TryGetValue(style, out var cached))
                return cached;

            var name = style switch
            {
                "bolditalic" => "BoldItalic",
                "bold" => "Bold",
                "italic" => "Italic",
                _ => "Regular",
            };
            using var stream = typeof(SystemFontResolver).Assembly
                .GetManifestResourceStream($"NetPdf.Fonts.LiberationSans-{name}.ttf");
            if (stream is null)
                return null;

            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return EmbeddedCache[style] = ms.ToArray();
        }
    }

    private string? FindFace(string familyName, bool bold, bool italic)
    {
        var files = _fontFiles.Value;
        var family = NormalizeAlnum(familyName);
        var style = StyleToken(bold, italic);

        var key = $"{family}|{style}";
        if (files.ContainsKey(key))
            return key;

        // Fall back to the regular face of the family; PDFsharp can simulate styles.
        key = $"{family}|regular";
        return files.ContainsKey(key) ? key : null;
    }

    internal static string StyleToken(bool bold, bool italic) => (bold, italic) switch
    {
        (true, true) => "bolditalic",
        (true, false) => "bold",
        (false, true) => "italic",
        _ => "regular",
    };

    // Strips every non-alphanumeric character so family names match regardless of
    // whether the source (requested family vs. on-disk filename) uses spaces,
    // hyphens, or no separator at all (e.g. "Liberation Sans" vs "LiberationSans-Regular").
    internal static string NormalizeAlnum(string name) =>
        new(name.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static Dictionary<string, string> ScanFontDirectories(string[] fontDirectories)
    {
        var map = new Dictionary<string, string>();
        foreach (var dir in fontDirectories)
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
