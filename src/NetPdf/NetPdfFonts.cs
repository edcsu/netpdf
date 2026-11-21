using NetPdf.Creation;

namespace NetPdf;

/// <summary>
/// Add your own fonts. Call this once at startup, before you build any PDF.
/// </summary>
/// <remarks>
/// Font lookup order: fonts you register, then fonts installed on the machine,
/// then the Liberation Sans that ships inside the package.
/// So a container with no fonts still works, with no extra setup.
/// </remarks>
public static class NetPdfFonts
{
    /// <summary>Registers a TrueType/OpenType font from raw bytes.</summary>
    public static void Register(string family, byte[] fontData, bool bold = false, bool italic = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(family);
        ArgumentNullException.ThrowIfNull(fontData);
        CustomFonts.Add(family, bold, italic, fontData);
    }

    /// <summary>Registers a TrueType/OpenType font from a file.</summary>
    public static void RegisterFile(string family, string path, bool bold = false, bool italic = false) =>
        Register(family, File.ReadAllBytes(path), bold, italic);
}
