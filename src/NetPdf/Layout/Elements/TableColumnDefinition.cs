namespace NetPdf.Layout.Elements;

/// <summary>One column of a <see cref="TableElement"/>: how its width is determined.</summary>
public sealed class TableColumnDefinition
{
    /// <summary>How the column width is determined. Defaults to a relative share.</summary>
    public RowItemType Type { get; init; } = RowItemType.Relative;

    /// <summary>The width in points for constant columns, or the weight for relative columns. Defaults to 1.</summary>
    public double Size { get; init; } = 1;
}
