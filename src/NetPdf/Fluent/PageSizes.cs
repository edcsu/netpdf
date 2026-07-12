using NetPdf.Layout;

namespace NetPdf.Fluent;

/// <summary>Common page sizes in points (portrait orientation).</summary>
public static class PageSizes
{
    /// <summary>ISO A3: 842 × 1191 points.</summary>
    public static Size A3 => new(842, 1191);

    /// <summary>ISO A4: 595 × 842 points.</summary>
    public static Size A4 => new(595, 842);

    /// <summary>ISO A5: 420 × 595 points.</summary>
    public static Size A5 => new(420, 595);

    /// <summary>US Letter: 612 × 792 points.</summary>
    public static Size Letter => new(612, 792);

    /// <summary>US Legal: 612 × 1008 points.</summary>
    public static Size Legal => new(612, 1008);
}
