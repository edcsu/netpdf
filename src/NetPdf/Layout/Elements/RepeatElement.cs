namespace NetPdf.Layout.Elements;

/// <summary>
/// Renders its content again on every page for as long as the surrounding context keeps
/// paginating. Because layout elements are single-use, the content is described by a factory
/// that is re-invoked after each completed render. The element never reports itself finished,
/// so it must be bounded by its context (e.g. combined with <see cref="StopPagingElement"/> or
/// placed alongside finite content); otherwise rendering stops with a max-page error.
/// </summary>
public sealed class RepeatElement : IElement
{
    private readonly Func<IElement> _factory;
    private IElement _current;

    /// <summary>Creates the element from a factory producing a fresh content tree per repetition.</summary>
    public RepeatElement(Func<IElement> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
        _current = factory();
    }

    /// <inheritdoc />
    public SpacePlan Measure(ICanvas canvas, Size availableSpace)
    {
        var plan = _current.Measure(canvas, availableSpace);
        // Even when the content fits, report a partial render so it repeats on the next page.
        return plan.Type == SpacePlanType.FullRender ? SpacePlan.PartialRender(plan.Size) : plan;
    }

    /// <inheritdoc />
    public void Draw(ICanvas canvas, Size availableSpace)
    {
        var plan = _current.Measure(canvas, availableSpace);
        _current.Draw(canvas, availableSpace);
        if (plan.Type == SpacePlanType.FullRender)
            _current = _factory();
    }
}
