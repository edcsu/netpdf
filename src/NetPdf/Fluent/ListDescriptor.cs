using NetPdf.Layout;
using NetPdf.Layout.Elements;

namespace NetPdf.Fluent;

/// <summary>
/// Builds bulleted or numbered lists as a column of marker/content rows. Unordered by default;
/// call <see cref="Ordered"/> for numbering.
/// </summary>
public sealed class ListDescriptor
{
    private readonly List<ItemSlot> _items = [];
    private string _bullet = "•";
    private string? _numberFormat;
    private double _spacing;
    private double _indent = 18;
    private TextStyle? _markerStyle;

    internal ListDescriptor()
    {
    }

    /// <summary>Uses a bullet marker for every item (default bullet is <c>•</c>).</summary>
    public ListDescriptor Unordered(string bullet = "•")
    {
        ArgumentException.ThrowIfNullOrEmpty(bullet);
        _bullet = bullet;
        _numberFormat = null;
        return this;
    }

    /// <summary>Numbers items with the given format, where <c>{0}</c> is the 1-based index.</summary>
    public ListDescriptor Ordered(string format = "{0}.")
    {
        ArgumentException.ThrowIfNullOrEmpty(format);
        _numberFormat = format;
        return this;
    }

    /// <summary>Sets the vertical gap between items in points.</summary>
    public ListDescriptor Spacing(double value)
    {
        _spacing = value;
        return this;
    }

    /// <summary>Sets the width of the marker column in points (default 18).</summary>
    public ListDescriptor Indent(double value)
    {
        _indent = value;
        return this;
    }

    /// <summary>Sets the text style of the markers.</summary>
    public ListDescriptor MarkerStyle(TextStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);
        _markerStyle = style;
        return this;
    }

    /// <summary>Adds a list item; configure its content on the returned descriptor.</summary>
    public ContainerDescriptor Item()
    {
        var slot = new ItemSlot();
        _items.Add(slot);
        return new ContainerDescriptor(element => slot.Element = element);
    }

    internal ColumnElement Build()
    {
        var column = new ColumnElement { Spacing = _spacing };
        for (var i = 0; i < _items.Count; i++)
        {
            var marker = _numberFormat is { } format
                ? string.Format(format, i + 1)
                : _bullet;
            var row = new RowElement();
            row.Items.Add(new RowItem
            {
                Type = RowItemType.Constant,
                Size = _indent,
                Element = new TextElement(marker, _markerStyle),
            });
            row.Items.Add(new RowItem { Element = _items[i].Element });
            column.Items.Add(row);
        }
        return column;
    }

    private sealed class ItemSlot
    {
        public IElement Element { get; set; } = new EmptyElement();
    }
}
