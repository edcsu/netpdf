namespace NetPdf.Layout.Elements;

/// <summary>
/// Flows items horizontally like words in a paragraph, wrapping to new rows when the width is
/// exhausted and continuing on the next page by whole rows. Every item must fit a page on its
/// own (items themselves never split).
/// </summary>
public sealed class InlinedElement : IElement
{
    private int _itemIndex;

    /// <summary>The flowed items, placed left to right, top to bottom.</summary>
    public IList<IElement> Items { get; init; } = [];

    /// <summary>Horizontal gap between items in a row, in points.</summary>
    public double HorizontalSpacing { get; set; }

    /// <summary>Vertical gap between rows, in points.</summary>
    public double VerticalSpacing { get; set; }

    /// <summary>How rows are aligned within the available width.</summary>
    public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;

    /// <summary>How items are aligned vertically within their row.</summary>
    public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Top;

    /// <inheritdoc />
    public SpacePlan Measure(ICanvas canvas, Size availableSpace)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        if (_itemIndex >= Items.Count)
            return SpacePlan.FullRender(Size.Zero);

        var (rows, placed, blocked) = BuildRows(canvas, availableSpace);
        if (rows.Count == 0)
            return SpacePlan.Wrap();

        var width = rows.Max(r => r.Width);
        var height = rows.Sum(r => r.Height) + (rows.Count - 1) * VerticalSpacing;
        var finished = !blocked && _itemIndex + placed >= Items.Count;
        var size = new Size(width, height);
        return finished ? SpacePlan.FullRender(size) : SpacePlan.PartialRender(size);
    }

    /// <inheritdoc />
    public void Draw(ICanvas canvas, Size availableSpace)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        var (rows, placed, _) = BuildRows(canvas, availableSpace);

        canvas.Save();
        var y = 0.0;
        foreach (var row in rows)
        {
            var x = Alignment switch
            {
                HorizontalAlignment.Center => (availableSpace.Width - row.Width) / 2,
                HorizontalAlignment.Right => availableSpace.Width - row.Width,
                _ => 0.0,
            };

            foreach (var (item, size) in row.Cells)
            {
                var dy = VerticalAlignment switch
                {
                    VerticalAlignment.Middle => (row.Height - size.Height) / 2,
                    VerticalAlignment.Bottom => row.Height - size.Height,
                    _ => 0.0,
                };

                canvas.Save();
                canvas.Translate(x, y + dy);
                item.Draw(canvas, size);
                canvas.Restore();
                x += size.Width + HorizontalSpacing;
            }

            y += row.Height + VerticalSpacing;
        }
        canvas.Restore();

        _itemIndex += placed;
    }

    private sealed class RowInfo
    {
        public List<(IElement Item, Size Size)> Cells { get; } = [];
        public double Width { get; set; }
        public double Height { get; set; }
    }

    // Packs items into rows fitting the available size, starting at the pagination cursor.
    // Returns the rows, the number of items placed, and whether packing stopped because an
    // item could not fully render (as opposed to running out of items).
    private (List<RowInfo> Rows, int Placed, bool Blocked) BuildRows(ICanvas canvas, Size availableSpace)
    {
        var rows = new List<RowInfo>();
        var current = new RowInfo();
        var usedHeight = 0.0;
        var placed = 0;
        var blocked = false;

        for (var i = _itemIndex; i < Items.Count; i++)
        {
            var plan = Items[i].Measure(canvas, availableSpace);
            if (plan.Type != SpacePlanType.FullRender)
            {
                blocked = true;
                break;
            }

            var size = plan.Size;
            var spacing = current.Cells.Count > 0 ? HorizontalSpacing : 0;
            if (current.Cells.Count > 0 && current.Width + spacing + size.Width > availableSpace.Width)
            {
                // Close the row; stop if the next row would overflow the height.
                usedHeight += current.Height + (rows.Count > 0 ? VerticalSpacing : 0);
                rows.Add(current);
                current = new RowInfo();
                spacing = 0;
            }

            var rowHeight = Math.Max(current.Height, size.Height);
            var vGap = rows.Count > 0 ? VerticalSpacing : 0;
            if (usedHeight + vGap + rowHeight > availableSpace.Height)
                break;

            current.Cells.Add((Items[i], size));
            current.Width += spacing + size.Width;
            current.Height = rowHeight;
            placed++;
        }

        if (current.Cells.Count > 0)
            rows.Add(current);
        return (rows, placed, blocked);
    }
}
