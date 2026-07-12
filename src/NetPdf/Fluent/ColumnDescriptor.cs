using NetPdf.Layout.Elements;

namespace NetPdf.Fluent;

/// <summary>Configures a vertical stack of items.</summary>
public sealed class ColumnDescriptor
{
    private readonly ColumnElement _column;

    internal ColumnDescriptor(ColumnElement column) => _column = column;

    /// <summary>Sets the vertical gap between consecutive items in points.</summary>
    public ColumnDescriptor Spacing(double value)
    {
        _column.Spacing = value;
        return this;
    }

    /// <summary>Adds an item and returns a descriptor to configure its content.</summary>
    public ContainerDescriptor Item() => new(element => _column.Items.Add(element));
}
