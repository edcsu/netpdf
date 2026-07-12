namespace NetPdf.Layout.Elements;

/// <summary>
/// Repeats a "before" element above and an "after" element below its content on every page the
/// content spans. Because layout elements are single-use, the repeated slots are described by
/// factories re-invoked per page; each must fully fit alongside the content.
/// </summary>
public sealed class DecorationElement : IElement
{
    /// <summary>Factory for the element drawn above the content on every page.</summary>
    public Func<IElement>? Before { get; init; }

    /// <summary>Factory for the element drawn below the content on every page.</summary>
    public Func<IElement>? After { get; init; }

    /// <summary>The flowing content between the repeated slots.</summary>
    public IElement Content { get; set; } = new EmptyElement();

    private IElement? _before;
    private IElement? _after;

    /// <inheritdoc />
    public SpacePlan Measure(ICanvas canvas, Size availableSpace)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        var (beforePlan, afterPlan) = MeasureSlots(canvas, availableSpace);
        if (beforePlan is { Type: not SpacePlanType.FullRender } ||
            afterPlan is { Type: not SpacePlanType.FullRender })
            return SpacePlan.Wrap();

        var slotHeight = (beforePlan?.Size.Height ?? 0) + (afterPlan?.Size.Height ?? 0);
        var remaining = availableSpace.Height - slotHeight;
        if (remaining <= 0)
            return SpacePlan.Wrap();

        var contentPlan = Content.Measure(canvas, new Size(availableSpace.Width, remaining));
        if (contentPlan.Type == SpacePlanType.Wrap)
            return SpacePlan.Wrap();

        var size = new Size(
            Math.Max(contentPlan.Size.Width,
                Math.Max(beforePlan?.Size.Width ?? 0, afterPlan?.Size.Width ?? 0)),
            contentPlan.Size.Height + slotHeight);
        return contentPlan.Type == SpacePlanType.PartialRender
            ? SpacePlan.PartialRender(size)
            : SpacePlan.FullRender(size);
    }

    /// <inheritdoc />
    public void Draw(ICanvas canvas, Size availableSpace)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        var (beforePlan, afterPlan) = MeasureSlots(canvas, availableSpace);
        var beforeHeight = beforePlan?.Size.Height ?? 0;
        var afterHeight = afterPlan?.Size.Height ?? 0;
        var contentSpace = new Size(availableSpace.Width,
            availableSpace.Height - beforeHeight - afterHeight);
        var contentPlan = Content.Measure(canvas, contentSpace);

        canvas.Save();
        _before?.Draw(canvas, new Size(availableSpace.Width, beforeHeight));
        canvas.Translate(0, beforeHeight);
        Content.Draw(canvas, contentSpace);
        canvas.Translate(0, contentPlan.Size.Height);
        _after?.Draw(canvas, new Size(availableSpace.Width, afterHeight));
        canvas.Restore();

        // Fresh slot instances next page; the drawn ones have consumed their state.
        _before = null;
        _after = null;
    }

    private (SpacePlan? Before, SpacePlan? After) MeasureSlots(ICanvas canvas, Size availableSpace)
    {
        _before ??= Before?.Invoke();
        _after ??= After?.Invoke();
        return (_before?.Measure(canvas, availableSpace), _after?.Measure(canvas, availableSpace));
    }
}
