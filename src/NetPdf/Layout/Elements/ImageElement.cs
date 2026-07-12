namespace NetPdf.Layout.Elements;

/// <summary>
/// Draws an image scaled to the available width, preserving its aspect ratio. The image is
/// atomic: it wraps to the next page when the scaled height does not fit. Constrain it with
/// sizing containers (Width, Height, AspectRatio) to change the fit behavior.
/// </summary>
public sealed class ImageElement(ImageSource source) : IElement
{
    private readonly ImageSource _source = source ?? throw new ArgumentNullException(nameof(source));

    /// <inheritdoc />
    public SpacePlan Measure(ICanvas canvas, Size availableSpace)
    {
        var size = ScaledSize(canvas, availableSpace);
        if (size.Width <= 0 || size.Height <= 0)
            return SpacePlan.FullRender(Size.Zero);
        return size.Height > availableSpace.Height ? SpacePlan.Wrap() : SpacePlan.FullRender(size);
    }

    /// <inheritdoc />
    public void Draw(ICanvas canvas, Size availableSpace)
    {
        var size = ScaledSize(canvas, availableSpace);
        if (size.Width <= 0 || size.Height <= 0)
            return;
        canvas.DrawImage(_source, 0, 0, size.Width, size.Height);
    }

    private Size ScaledSize(ICanvas canvas, Size availableSpace)
    {
        var intrinsic = canvas.MeasureImage(_source);
        if (intrinsic.Width <= 0 || intrinsic.Height <= 0 || availableSpace.Width <= 0)
            return Size.Zero;

        var width = Math.Min(intrinsic.Width, availableSpace.Width);
        return new Size(width, width * intrinsic.Height / intrinsic.Width);
    }
}
