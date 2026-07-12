namespace NetPdf.Layout;

/// <summary>A width/height pair in PDF points (1/72 inch).</summary>
public readonly struct Size
{
    /// <summary>The width in points.</summary>
    public double Width { get; }

    /// <summary>The height in points.</summary>
    public double Height { get; }

    /// <summary>Creates a size. Both dimensions must be non-negative.</summary>
    public Size(double width, double height)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        ArgumentOutOfRangeException.ThrowIfNegative(height);
        Width = width;
        Height = height;
    }

    /// <summary>A size with zero width and height.</summary>
    public static Size Zero => new(0, 0);

    /// <summary>An effectively unlimited size, used to measure unconstrained content.</summary>
    public static Size Max => new(1_000_000, 1_000_000);
}
