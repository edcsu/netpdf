namespace NetPdf.Layout;

/// <summary>
/// Describes a drop shadow painted behind content. PDF has no native blur, so the shadow is
/// approximated: <see cref="Blur"/> greater than zero draws a few concentric translucent
/// rectangles expanding outward; zero draws a single solid offset rectangle.
/// </summary>
public sealed record ShadowStyle
{
    /// <summary>Shadow color; defaults to black at 25% opacity.</summary>
    public System.Drawing.Color Color { get; init; } = System.Drawing.Color.FromArgb(64, 0, 0, 0);

    /// <summary>Horizontal shadow offset in points.</summary>
    public double OffsetX { get; init; } = 2;

    /// <summary>Vertical shadow offset in points.</summary>
    public double OffsetY { get; init; } = 2;

    /// <summary>
    /// Approximate blur radius in points. Rendered as stepped translucent rectangles, not a
    /// true Gaussian blur. Zero renders one solid rectangle.
    /// </summary>
    public double Blur { get; init; }

    /// <summary>Corner radius of the shadow rectangle, typically matching the content's background.</summary>
    public double CornerRadius { get; init; }
}
