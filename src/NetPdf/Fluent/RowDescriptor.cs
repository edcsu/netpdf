using NetPdf.Layout.Elements;

namespace NetPdf.Fluent;

/// <summary>Configures a horizontal arrangement of items.</summary>
public sealed class RowDescriptor
{
    private readonly RowElement _row;

    internal RowDescriptor(RowElement row) => _row = row;

    /// <summary>Sets the horizontal gap between consecutive items in points.</summary>
    public RowDescriptor Spacing(double value)
    {
        _row.Spacing = value;
        return this;
    }

    /// <summary>Adds an item with an exact width in points.</summary>
    public ContainerDescriptor ConstantItem(double width) =>
        AddItem(new RowItem { Type = RowItemType.Constant, Size = width });

    /// <summary>Adds an item taking a weighted share of the remaining width.</summary>
    public ContainerDescriptor RelativeItem(double weight = 1) =>
        AddItem(new RowItem { Type = RowItemType.Relative, Size = weight });

    private ContainerDescriptor AddItem(RowItem item)
    {
        _row.Items.Add(item);
        return new ContainerDescriptor(element => item.Element = element);
    }
}
