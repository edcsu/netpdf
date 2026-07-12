namespace NetPdf.Layout.Elements;

/// <summary>Direction of a <see cref="LineElement"/>.</summary>
public enum LineOrientation
{
    /// <summary>Spans the available width; occupies the line thickness vertically.</summary>
    Horizontal,
    /// <summary>Spans the available height; occupies the line thickness horizontally.</summary>
    Vertical,
}

/// <summary>Draws a straight rule across the available space.</summary>
public sealed class LineElement : IElement
{
    /// <summary>Whether the line runs horizontally or vertically.</summary>
    public LineOrientation Orientation { get; init; } = LineOrientation.Horizontal;

    /// <summary>Stroke thickness in points.</summary>
    public double Thickness { get; init; } = 1;

    /// <summary>Line color.</summary>
    public System.Drawing.Color Color { get; init; } = System.Drawing.Color.Black;

    /// <inheritdoc />
    public SpacePlan Measure(ICanvas canvas, Size availableSpace) =>
        Orientation == LineOrientation.Horizontal
            ? availableSpace.Height < Thickness
                ? SpacePlan.Wrap()
                : SpacePlan.FullRender(new Size(availableSpace.Width, Thickness))
            : availableSpace.Width < Thickness
                ? SpacePlan.Wrap()
                : SpacePlan.FullRender(new Size(Thickness, availableSpace.Height));

    /// <inheritdoc />
    public void Draw(ICanvas canvas, Size availableSpace)
    {
        var mid = Thickness / 2;
        if (Orientation == LineOrientation.Horizontal)
            canvas.DrawLine(0, mid, availableSpace.Width, mid, Color, Thickness);
        else
            canvas.DrawLine(mid, 0, mid, availableSpace.Height, Color, Thickness);
    }
}
