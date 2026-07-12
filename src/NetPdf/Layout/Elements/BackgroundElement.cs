namespace NetPdf.Layout.Elements;

/// <summary>
/// Paints a solid background behind its child. Measurement passes through unchanged; the
/// rectangle covers the space the child occupies on the current page.
/// </summary>
public sealed class BackgroundElement : ContainerElement
{
    /// <summary>Fill color.</summary>
    public System.Drawing.Color Color { get; init; }

    /// <summary>Corner radius in points; zero for square corners.</summary>
    public double CornerRadius { get; init; }

    /// <inheritdoc />
    public override void Draw(ICanvas canvas, Size availableSpace)
    {
        var plan = Child.Measure(canvas, availableSpace);
        if (plan.Type == SpacePlanType.Wrap)
            return;

        canvas.DrawRectangle(0, 0, plan.Size.Width, plan.Size.Height, fill: Color, cornerRadius: CornerRadius);
        Child.Draw(canvas, availableSpace);
    }
}
