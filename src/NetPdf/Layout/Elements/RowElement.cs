namespace NetPdf.Layout.Elements;

/// <summary>How a <see cref="RowItem"/>'s width is determined.</summary>
public enum RowItemType
{
    /// <summary>The item takes an exact width in points.</summary>
    Constant,
    /// <summary>The item takes a weighted share of the width left after constant items.</summary>
    Relative,
}

/// <summary>One cell of a <see cref="RowElement"/>: an element plus its width sizing.</summary>
public sealed class RowItem
{
    /// <summary>The element drawn in this cell. Defaults to an <see cref="EmptyElement"/>.</summary>
    public IElement Element { get; set; } = new EmptyElement();

    /// <summary>How the cell width is determined. Defaults to a relative share.</summary>
    public RowItemType Type { get; init; } = RowItemType.Relative;

    /// <summary>The width in points for constant items, or the weight for relative items. Defaults to 1.</summary>
    public double Size { get; init; } = 1;
}

/// <summary>
/// Places items side by side with optional spacing. Column widths are fixed across pages:
/// constant items take their exact width and relative items share the remainder by weight.
/// A row continues on the next page while any item still has content, and items that already
/// finished are not drawn again.
/// </summary>
public sealed class RowElement : IElement
{
    private bool[]? _finished;

    /// <summary>The row cells, drawn left to right.</summary>
    public IList<RowItem> Items { get; init; } = [];

    /// <summary>Horizontal gap between consecutive items in points.</summary>
    public double Spacing { get; init; }

    /// <inheritdoc />
    public SpacePlan Measure(ICanvas canvas, Size availableSpace)
    {
        if (Items.Count == 0)
            return SpacePlan.FullRender(Size.Zero);

        _finished ??= new bool[Items.Count];
        var widths = AllocateWidths(availableSpace.Width);
        var height = 0.0;
        var anyPartial = false;
        for (var i = 0; i < Items.Count; i++)
        {
            if (_finished[i])
                continue;

            var plan = Items[i].Element.Measure(canvas, new Size(widths[i], availableSpace.Height));
            switch (plan.Type)
            {
                case SpacePlanType.Wrap:
                    return SpacePlan.Wrap();
                case SpacePlanType.PartialRender:
                    anyPartial = true;
                    break;
            }
            height = Math.Max(height, plan.Size.Height);
        }

        var totalWidth = widths.Sum() + Spacing * (Items.Count - 1);
        var size = new Size(totalWidth, height);
        return anyPartial ? SpacePlan.PartialRender(size) : SpacePlan.FullRender(size);
    }

    /// <inheritdoc />
    public void Draw(ICanvas canvas, Size availableSpace)
    {
        if (Items.Count == 0)
            return;

        _finished ??= new bool[Items.Count];
        var widths = AllocateWidths(availableSpace.Width);
        canvas.Save();
        for (var i = 0; i < Items.Count; i++)
        {
            if (!_finished[i])
            {
                var itemSpace = new Size(widths[i], availableSpace.Height);
                var plan = Items[i].Element.Measure(canvas, itemSpace);
                if (plan.Type != SpacePlanType.Wrap)
                {
                    Items[i].Element.Draw(canvas, itemSpace);
                    if (plan.Type == SpacePlanType.FullRender)
                        _finished[i] = true;
                }
            }
            canvas.Translate(widths[i] + Spacing, 0);
        }
        canvas.Restore();
    }

    private double[] AllocateWidths(double availableWidth)
    {
        var constants = Items.Where(i => i.Type == RowItemType.Constant).Sum(i => i.Size);
        var totalWeight = Items.Where(i => i.Type == RowItemType.Relative).Sum(i => i.Size);
        var remainder = Math.Max(0, availableWidth - constants - Spacing * (Items.Count - 1));
        return Items
            .Select(i => i.Type == RowItemType.Constant
                ? i.Size
                : totalWeight <= 0 ? 0 : remainder * i.Size / totalWeight)
            .ToArray();
    }
}
