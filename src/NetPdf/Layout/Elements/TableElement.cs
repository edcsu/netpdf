namespace NetPdf.Layout.Elements;

/// <summary>
/// A grid of cells with fixed column widths, optional cell spans and header/footer rows
/// repeated on every page. Pagination happens between <em>row bands</em> — groups of rows
/// closed under intersecting row spans — so no cell ever straddles a page break; a band too
/// tall for an empty page fails with a <see cref="LayoutException"/>.
/// </summary>
public sealed class TableElement : IElement
{
    // "Unlimited" height used to find a cell's natural size; kept finite so canvases can
    // still do integer line math on it.
    private const double UnboundedHeight = 1e8;
    private const double Epsilon = 1e-6;

    private int _currentBand;

    /// <summary>The column definitions; their count is the table's column count.</summary>
    public IList<TableColumnDefinition> Columns { get; init; } = [];

    /// <summary>The body cells.</summary>
    public IList<TableCell> Cells { get; init; } = [];

    /// <summary>
    /// Factory producing the header cells repeated at the top of the table on every page.
    /// Invoked once per measure/draw because elements are single-use.
    /// </summary>
    public Func<IReadOnlyList<TableCell>>? HeaderFactory { get; set; }

    /// <summary>
    /// Factory producing the footer cells repeated below the body on every page.
    /// Invoked once per measure/draw because elements are single-use.
    /// </summary>
    public Func<IReadOnlyList<TableCell>>? FooterFactory { get; set; }

    /// <inheritdoc />
    public SpacePlan Measure(ICanvas canvas, Size availableSpace)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        if (Columns.Count == 0)
            return SpacePlan.FullRender(Size.Zero);

        var widths = AllocateWidths(availableSpace.Width);
        var headerHeight = SectionGrid(canvas, HeaderFactory, widths)?.TotalHeight ?? 0;
        var footerHeight = SectionGrid(canvas, FooterFactory, widths)?.TotalHeight ?? 0;
        var body = BuildGrid(canvas, Cells, widths);

        var available = availableSpace.Height - headerHeight - footerHeight;
        var taken = TakeBands(body, available, out var bodyUsed);
        if (taken == 0 && _currentBand < body.Bands.Count)
            return SpacePlan.Wrap();

        var size = new Size(widths.Sum(), headerHeight + footerHeight + bodyUsed);
        return _currentBand + taken >= body.Bands.Count
            ? SpacePlan.FullRender(size)
            : SpacePlan.PartialRender(size);
    }

    /// <inheritdoc />
    public void Draw(ICanvas canvas, Size availableSpace)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        if (Columns.Count == 0)
            return;

        var widths = AllocateWidths(availableSpace.Width);
        var header = SectionGrid(canvas, HeaderFactory, widths);
        var footer = SectionGrid(canvas, FooterFactory, widths);
        var body = BuildGrid(canvas, Cells, widths);

        var available = availableSpace.Height - (header?.TotalHeight ?? 0) - (footer?.TotalHeight ?? 0);
        var taken = TakeBands(body, available, out _);

        canvas.Save();
        if (header is not null)
        {
            DrawRows(canvas, header, 0, header.RowHeights.Length - 1, widths);
            canvas.Translate(0, header.TotalHeight);
        }

        for (var i = _currentBand; i < _currentBand + taken; i++)
        {
            var (start, end, height) = body.Bands[i];
            DrawRows(canvas, body, start, end, widths);
            canvas.Translate(0, height);
        }

        if (footer is not null)
            DrawRows(canvas, footer, 0, footer.RowHeights.Length - 1, widths);
        canvas.Restore();

        _currentBand += taken;
    }

    private int TakeBands(Grid body, double available, out double used)
    {
        used = 0;
        var taken = 0;
        for (var i = _currentBand; i < body.Bands.Count; i++)
        {
            var height = body.Bands[i].Height;
            if (used + height > available + Epsilon)
                break;
            used += height;
            taken++;
        }

        return taken;
    }

    private static void DrawRows(ICanvas canvas, Grid grid, int firstRow, int lastRow, double[] widths)
    {
        foreach (var cell in grid.Cells)
        {
            if (cell.PlacedRow < firstRow || cell.PlacedRow > lastRow)
                continue;

            var x = widths.Take(cell.PlacedColumn).Sum();
            var y = SumRange(grid.RowHeights, firstRow, cell.PlacedRow);
            var size = new Size(
                SumRange(widths, cell.PlacedColumn, cell.PlacedColumn + cell.ColumnSpan),
                SumRange(grid.RowHeights, cell.PlacedRow, cell.PlacedRow + cell.RowSpan));

            canvas.Save();
            canvas.Translate(x, y);
            cell.Element.Draw(canvas, size);
            canvas.Restore();
        }
    }

    private static Grid? SectionGrid(ICanvas canvas, Func<IReadOnlyList<TableCell>>? factory, double[] widths)
    {
        if (factory is null)
            return null;
        var cells = factory();
        return cells.Count == 0 ? null : BuildGrid(canvas, cells, widths);
    }

    private static Grid BuildGrid(ICanvas canvas, IEnumerable<TableCell> cellSource, double[] widths)
    {
        var cells = cellSource.ToList();
        Place(cells, widths.Length);

        var rowCount = cells.Count == 0 ? 0 : cells.Max(c => c.PlacedRow + c.RowSpan);
        var heights = new double[rowCount];

        foreach (var cell in cells.Where(c => c.RowSpan == 1))
            heights[cell.PlacedRow] = Math.Max(heights[cell.PlacedRow], NaturalHeight(canvas, cell, widths));

        // Spanning cells stretch their last spanned row when the spanned rows come up short.
        foreach (var cell in cells.Where(c => c.RowSpan > 1).OrderBy(c => c.PlacedRow + c.RowSpan))
        {
            var deficit = NaturalHeight(canvas, cell, widths)
                - SumRange(heights, cell.PlacedRow, cell.PlacedRow + cell.RowSpan);
            if (deficit > 0)
                heights[cell.PlacedRow + cell.RowSpan - 1] += deficit;
        }

        return new Grid(cells, heights, ComputeBands(cells, heights));
    }

    private static double NaturalHeight(ICanvas canvas, TableCell cell, double[] widths)
    {
        var width = SumRange(widths, cell.PlacedColumn, cell.PlacedColumn + cell.ColumnSpan);
        var plan = cell.Element.Measure(canvas, new Size(width, UnboundedHeight));
        if (plan.Type == SpacePlanType.Wrap)
            throw new LayoutException("A table cell refused to render even with unlimited height.");
        return plan.Size.Height;
    }

    private static void Place(List<TableCell> cells, int columnCount)
    {
        var occupied = new HashSet<(int Row, int Col)>();

        void Occupy(TableCell cell)
        {
            for (var r = cell.PlacedRow; r < cell.PlacedRow + cell.RowSpan; r++)
                for (var c = cell.PlacedColumn; c < cell.PlacedColumn + cell.ColumnSpan; c++)
                    if (!occupied.Add((r, c)))
                        throw new LayoutException($"Table cells overlap at row {r + 1}, column {c + 1}.");
        }

        bool IsFree(int row, int col, TableCell cell)
        {
            for (var r = row; r < row + cell.RowSpan; r++)
                for (var c = col; c < col + cell.ColumnSpan; c++)
                    if (occupied.Contains((r, c)))
                        return false;
            return true;
        }

        foreach (var cell in cells)
        {
            if (cell.RowSpan < 1 || cell.ColumnSpan < 1)
                throw new LayoutException("Table cell spans must be at least 1.");
            if (cell.ColumnSpan > columnCount)
                throw new LayoutException(
                    $"A table cell spans {cell.ColumnSpan} columns but the table has only {columnCount}.");
            if (cell.Row.HasValue != cell.Column.HasValue)
                throw new LayoutException("Explicitly placed table cells must set both Row and Column.");
        }

        foreach (var cell in cells.Where(c => c.Row.HasValue))
        {
            cell.PlacedRow = cell.Row!.Value - 1;
            cell.PlacedColumn = cell.Column!.Value - 1;
            if (cell.PlacedRow < 0 || cell.PlacedColumn < 0)
                throw new LayoutException("Table cell Row and Column are 1-based and must be positive.");
            if (cell.PlacedColumn + cell.ColumnSpan > columnCount)
                throw new LayoutException(
                    $"A table cell at column {cell.Column} with span {cell.ColumnSpan} exceeds the table's {columnCount} columns.");
            Occupy(cell);
        }

        var (row, col) = (0, 0);
        foreach (var cell in cells.Where(c => !c.Row.HasValue))
        {
            while (col + cell.ColumnSpan > columnCount || !IsFree(row, col, cell))
            {
                col++;
                if (col >= columnCount)
                    (row, col) = (row + 1, 0);
            }

            cell.PlacedRow = row;
            cell.PlacedColumn = col;
            Occupy(cell);
        }
    }

    // Groups rows into maximal runs closed under intersecting row spans; pagination only
    // happens between bands, so no cell straddles a page break.
    private static List<(int Start, int End, double Height)> ComputeBands(
        List<TableCell> cells, double[] heights)
    {
        var bands = new List<(int, int, double)>();
        var start = 0;
        while (start < heights.Length)
        {
            var end = start;
            bool grew;
            do
            {
                grew = false;
                foreach (var cell in cells)
                {
                    var last = cell.PlacedRow + cell.RowSpan - 1;
                    if (cell.PlacedRow <= end && last > end)
                    {
                        end = last;
                        grew = true;
                    }
                }
            }
            while (grew);

            bands.Add((start, end, SumRange(heights, start, end + 1)));
            start = end + 1;
        }

        return bands;
    }

    private double[] AllocateWidths(double availableWidth)
    {
        var constants = Columns.Where(c => c.Type == RowItemType.Constant).Sum(c => c.Size);
        var totalWeight = Columns.Where(c => c.Type == RowItemType.Relative).Sum(c => c.Size);
        var remainder = Math.Max(0, availableWidth - constants);
        return Columns
            .Select(c => c.Type == RowItemType.Constant
                ? c.Size
                : totalWeight <= 0 ? 0 : remainder * c.Size / totalWeight)
            .ToArray();
    }

    private static double SumRange(double[] values, int from, int toExclusive)
    {
        var sum = 0.0;
        for (var i = from; i < toExclusive && i < values.Length; i++)
            sum += values[i];
        return sum;
    }

    private sealed record Grid(
        List<TableCell> Cells,
        double[] RowHeights,
        List<(int Start, int End, double Height)> Bands)
    {
        public double TotalHeight => RowHeights.Sum();
    }
}
