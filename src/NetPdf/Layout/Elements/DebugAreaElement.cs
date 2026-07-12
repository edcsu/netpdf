namespace NetPdf.Layout.Elements;

/// <summary>
/// Draws its child normally, then overlays a colored outline and an optional label on the
/// area the child occupied. A layout diagnostic aid: render the page to PNG (via
/// <c>PdfDocument.RenderPage</c>) to inspect element boundaries. The overlay consumes no
/// layout space and does not affect measurement.
/// </summary>
public sealed class DebugAreaElement : ContainerElement
{
    /// <summary>Fixed palette used when no explicit color is given; picked by label hash.</summary>
    private static readonly System.Drawing.Color[] Palette =
    [
        System.Drawing.Color.Red,
        System.Drawing.Color.Blue,
        System.Drawing.Color.Green,
        System.Drawing.Color.DarkOrange,
        System.Drawing.Color.Purple,
        System.Drawing.Color.Teal,
        System.Drawing.Color.Magenta,
        System.Drawing.Color.Brown,
    ];

    /// <summary>Label drawn in the top-left corner of the outlined area. Empty draws no label.</summary>
    public string Label { get; set; } = "";

    /// <summary>Outline and label color. When null, a palette color is picked from the label hash.</summary>
    public System.Drawing.Color? Color { get; set; }

    /// <inheritdoc />
    public override void Draw(ICanvas canvas, Size availableSpace)
    {
        // Measure before drawing: elements are stateful and advance their paging
        // position during Draw, so measuring afterwards reports the next page's slice.
        var plan = Child.Measure(canvas, availableSpace);
        bool measured = plan.Type is SpacePlanType.FullRender or SpacePlanType.PartialRender;
        double width = measured ? plan.Size.Width : availableSpace.Width;
        double height = measured ? plan.Size.Height : availableSpace.Height;

        Child.Draw(canvas, availableSpace);

        var color = Color ?? Palette[Math.Abs(string.GetHashCode(Label, StringComparison.Ordinal)) % Palette.Length];
        canvas.DrawRectangle(0, 0, width, height, fill: null, stroke: color, strokeThickness: 1);

        if (Label.Length > 0)
        {
            var labelStyle = new TextStyle { FontSize = 6, Color = color };
            canvas.DrawText(Label, labelStyle.Merge(canvas.DefaultTextStyle).Resolve(), 1, 0);
        }
    }
}
