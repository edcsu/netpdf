namespace NetPdf.Layout.Elements;

/// <summary>
/// Paints an approximated drop shadow behind its child. Measurement passes through unchanged;
/// the shadow is visual only and may extend past the slot. See <see cref="ShadowStyle"/> for
/// the blur approximation caveat.
/// </summary>
public sealed class ShadowElement : ContainerElement
{
    private const int BlurSteps = 3;

    /// <summary>The shadow's appearance.</summary>
    public ShadowStyle Style { get; init; } = new();

    /// <inheritdoc />
    public override void Draw(ICanvas canvas, Size availableSpace)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        var plan = Child.Measure(canvas, availableSpace);
        if (plan.Type == SpacePlanType.Wrap)
            return;

        var (w, h) = (plan.Size.Width, plan.Size.Height);
        if (Style.Blur <= 0)
        {
            DrawShadowRect(canvas, 0, w, h, Style.Color);
        }
        else
        {
            // Fake blur: concentric rectangles growing by Blur/steps, each carrying an equal
            // share of the alpha so the edge fades outward.
            var alpha = Math.Max(1, Style.Color.A / BlurSteps);
            var faded = System.Drawing.Color.FromArgb(alpha, Style.Color.R, Style.Color.G, Style.Color.B);
            for (var i = 0; i < BlurSteps; i++)
                DrawShadowRect(canvas, Style.Blur * i / BlurSteps, w, h, faded);
        }

        Child.Draw(canvas, availableSpace);
    }

    private void DrawShadowRect(ICanvas canvas, double spread, double width, double height,
        System.Drawing.Color color) =>
        canvas.DrawRectangle(
            Style.OffsetX - spread, Style.OffsetY - spread,
            width + spread * 2, height + spread * 2,
            fill: color, cornerRadius: Style.CornerRadius + spread);
}
