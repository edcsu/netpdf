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
        var style = (bold, italic) switch
        {
            (true, true) => " bold italic",
            (true, false) => " bold",
            (false, true) => " italic",
            _ => "",
        };

        var key = Normalize(familyName + style);
        if (files.ContainsKey(key))
            return key;

        // Fall back to the regular face of the family; PDFsharp can simulate styles.
        key = Normalize(familyName);
        return files.ContainsKey(key) ? key : null;
    }

    private static string Normalize(string name) =>
        name.Replace("-", " ").Replace("_", " ").ToLowerInvariant().Trim();

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

                var key = Normalize(Path.GetFileNameWithoutExtension(file));
                map.TryAdd(key, file);
            }
        }

        return map;
    }
}
