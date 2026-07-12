namespace NetPdf.Layout.Elements;

/// <summary>
/// Strokes a border on the edges of its child. The border is drawn on the content edge and
/// consumes no layout space; add Padding inside to keep content clear of it.
/// </summary>
public sealed class BorderElement : ContainerElement
{
    /// <summary>Left border thickness in points.</summary>
    public double Left { get; init; }

    /// <summary>Top border thickness in points.</summary>
    public double Top { get; init; }

    /// <summary>Right border thickness in points.</summary>
    public double Right { get; init; }

    /// <summary>Bottom border thickness in points.</summary>
    public double Bottom { get; init; }

    /// <summary>Border color.</summary>
    public System.Drawing.Color Color { get; init; } = System.Drawing.Color.Black;

    /// <inheritdoc />
    public override void Draw(ICanvas canvas, Size availableSpace)
    {
        var plan = Child.Measure(canvas, availableSpace);
        if (plan.Type == SpacePlanType.Wrap)
            return;

        var (w, h) = (plan.Size.Width, plan.Size.Height);
        if (Left == Top && Top == Right && Right == Bottom)
        {
            if (Left > 0)
                canvas.DrawRectangle(Left / 2, Left / 2, w - Left, h - Left,
                    fill: null, stroke: Color, strokeThickness: Left);
        }
        else
        {
            // Each side is inset by half its thickness so the stroke stays on the content edge.
            if (Left > 0)
                canvas.DrawLine(Left / 2, 0, Left / 2, h, Color, Left);
            if (Top > 0)
                canvas.DrawLine(0, Top / 2, w, Top / 2, Color, Top);
            if (Right > 0)
                canvas.DrawLine(w - Right / 2, 0, w - Right / 2, h, Color, Right);
            if (Bottom > 0)
                canvas.DrawLine(0, h - Bottom / 2, w, h - Bottom / 2, Color, Bottom);
        }

        Child.Draw(canvas, availableSpace);
    }
}
