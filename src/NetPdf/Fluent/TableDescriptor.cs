using NetPdf.Layout.Elements;

namespace NetPdf.Fluent;

/// <summary>Configures a table: column definitions, body cells and repeating header/footer rows.</summary>
public sealed class TableDescriptor
{
    private readonly TableElement _table = new();

    internal TableDescriptor()
    {
    }

    /// <summary>Defines the table's columns; must be called before the table renders.</summary>
    public void ColumnsDefinition(Action<TableColumnsDefinitionDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(new TableColumnsDefinitionDescriptor(_table.Columns));
    }

    /// <summary>
    /// Adds a body cell. Chain <see cref="TableCellDescriptor.Row"/>/<see cref="TableCellDescriptor.Column"/>
    /// for explicit placement, or leave unplaced to fill the next free slot.
    /// </summary>
    public TableCellDescriptor Cell()
    {
        var cell = new TableCell();
        _table.Cells.Add(cell);
        return new TableCellDescriptor(cell);
    }

    /// <summary>Defines header cells repeated at the top of the table on every page.</summary>
    public void Header(Action<TableSectionDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        // Elements are single-use, so the section is rebuilt from the configuration per page.
        _table.HeaderFactory = () => BuildSection(configure);
    }

    /// <summary>Defines footer cells repeated below the table body on every page.</summary>
    public void Footer(Action<TableSectionDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _table.FooterFactory = () => BuildSection(configure);
    }

    internal TableElement Build() => _table;

    private static List<TableCell> BuildSection(Action<TableSectionDescriptor> configure)
    {
        var cells = new List<TableCell>();
        configure(new TableSectionDescriptor(cells));
        return cells;
    }
}

/// <summary>Configures the columns of a table.</summary>
public sealed class TableColumnsDefinitionDescriptor
{
    private readonly IList<TableColumnDefinition> _columns;

    internal TableColumnsDefinitionDescriptor(IList<TableColumnDefinition> columns) => _columns = columns;

    /// <summary>Adds a column with an exact width in points.</summary>
    public void ConstantColumn(double width) =>
        _columns.Add(new TableColumnDefinition { Type = RowItemType.Constant, Size = width });

    /// <summary>Adds a column taking a weighted share of the width left after constant columns.</summary>
    public void RelativeColumn(double weight = 1) =>
        _columns.Add(new TableColumnDefinition { Type = RowItemType.Relative, Size = weight });
}

/// <summary>Configures the cells of a repeating table header or footer.</summary>
public sealed class TableSectionDescriptor
{
    private readonly IList<TableCell> _cells;

    internal TableSectionDescriptor(IList<TableCell> cells) => _cells = cells;

    /// <summary>Adds a cell to the section.</summary>
    public TableCellDescriptor Cell()
    {
        var cell = new TableCell();
        _cells.Add(cell);
        return new TableCellDescriptor(cell);
    }
}

/// <summary>Configures one table cell: placement, spans and content.</summary>
public sealed class TableCellDescriptor
{
    private readonly TableCell _cell;

    internal TableCellDescriptor(TableCell cell) => _cell = cell;

    /// <summary>Places the cell at a 1-based row; set together with <see cref="Column"/>.</summary>
    public TableCellDescriptor Row(int row)
    {
        _cell.Row = row;
        return this;
    }

    /// <summary>Places the cell at a 1-based column; set together with <see cref="Row"/>.</summary>
    public TableCellDescriptor Column(int column)
    {
        _cell.Column = column;
        return this;
    }

    /// <summary>Spans the cell across the given number of rows.</summary>
    public TableCellDescriptor RowSpan(int rows)
    {
        _cell.RowSpan = rows;
        return this;
    }

    /// <summary>Spans the cell across the given number of columns.</summary>
    public TableCellDescriptor ColumnSpan(int columns)
    {
        _cell.ColumnSpan = columns;
        return this;
    }

    /// <summary>Configures the cell's content.</summary>
    public ContainerDescriptor Element() => new(e => _cell.Element = e);

    /// <summary>Places word-wrapping text in the cell.</summary>
    public void Text(string text, Layout.TextStyle? style = null) => Element().Text(text, style);
}
