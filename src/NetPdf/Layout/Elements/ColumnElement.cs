namespace NetPdf.Layout.Elements;

/// <summary>
/// Stacks items vertically with optional spacing, continuing on the next page when the
/// remaining items do not fit.
/// </summary>
public sealed class ColumnElement : IElement
{
    private int _currentIndex;

    /// <summary>The stacked items, drawn top to bottom.</summary>
    public IList<IElement> Items { get; init; } = [];

    /// <summary>Vertical gap between consecutive items in points.</summary>
    public double Spacing { get; set; }

    /// <inheritdoc />
    public SpacePlan Measure(ICanvas canvas, Size availableSpace)
    {
        if (_currentIndex >= Items.Count)
            return SpacePlan.FullRender(Size.Zero);

        var used = 0.0;
        var width = 0.0;
        for (var i = _currentIndex; i < Items.Count; i++)
        {
            var offset = i == _currentIndex ? 0 : Spacing;
            var remaining = availableSpace.Height - used - offset;
            if (remaining <= 0)
                return SpacePlan.PartialRender(new Size(width, used));

            var plan = Items[i].Measure(canvas, new Size(availableSpace.Width, remaining));
            switch (plan.Type)
            {
                case SpacePlanType.Wrap:
                    return used <= 0
                        ? SpacePlan.Wrap()
                        : SpacePlan.PartialRender(new Size(width, used));

                case SpacePlanType.PartialRender:
                    return SpacePlan.PartialRender(new Size(
                        Math.Max(width, plan.Size.Width),
                        used + offset + plan.Size.Height));

                case SpacePlanType.FullRender:
                    used += offset + plan.Size.Height;
                    width = Math.Max(width, plan.Size.Width);
                    break;
            }
        }

        return SpacePlan.FullRender(new Size(width, used));
    }

    /// <inheritdoc />
    public void Draw(ICanvas canvas, Size availableSpace)
    {
        canvas.Save();
        var used = 0.0;
        while (_currentIndex < Items.Count)
        {
            var offset = used <= 0 ? 0 : Spacing;
            var remaining = availableSpace.Height - used - offset;
            if (remaining <= 0)
                break;

            var item = Items[_currentIndex];
            var itemSpace = new Size(availableSpace.Width, remaining);
            var plan = item.Measure(canvas, itemSpace);
            if (plan.Type == SpacePlanType.Wrap)
                break;

            canvas.Translate(0, offset);
            item.Draw(canvas, itemSpace);
            canvas.Translate(0, plan.Size.Height);
            used += offset + plan.Size.Height;

            if (plan.Type == SpacePlanType.PartialRender)
                break; // The item continues on the next page and stays current.

            _currentIndex++;
        }
        canvas.Restore();
    }
}
