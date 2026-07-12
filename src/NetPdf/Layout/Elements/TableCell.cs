namespace NetPdf.Layout.Elements;

/// <summary>
/// One cell of a <see cref="TableElement"/>. Set <see cref="Row"/> and <see cref="Column"/>
/// for explicit placement (1-based); leave both null to auto-place the cell in the next free
/// slot, scanning left to right, top to bottom.
/// </summary>
public sealed class TableCell
{
    /// <summary>The cell content. Defaults to an <see cref="EmptyElement"/>.</summary>
    public IElement Element { get; set; } = new EmptyElement();

    /// <summary>1-based row for explicit placement, or null to auto-place. Set together with <see cref="Column"/>.</summary>
    public int? Row { get; set; }

    /// <summary>1-based column for explicit placement, or null to auto-place. Set together with <see cref="Row"/>.</summary>
    public int? Column { get; set; }

    /// <summary>Number of rows the cell spans. Defaults to 1.</summary>
    public int RowSpan { get; set; } = 1;

    /// <summary>Number of columns the cell spans. Defaults to 1.</summary>
    public int ColumnSpan { get; set; } = 1;

    /// <summary>0-based row index resolved during placement.</summary>
    internal int PlacedRow { get; set; }

    /// <summary>0-based column index resolved during placement.</summary>
    internal int PlacedColumn { get; set; }
}
