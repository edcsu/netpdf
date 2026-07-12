using NetPdf.Layout;
using NetPdf.Layout.Elements;

namespace NetPdf.Fluent;

/// <summary>Configures items flowed horizontally with wrapping, like words in a paragraph.</summary>
public sealed class InlinedDescriptor
{
    private readonly InlinedElement _inlined;

    internal InlinedDescriptor(InlinedElement inlined) => _inlined = inlined;

    /// <summary>Sets the same gap between items and between rows in points.</summary>
    public InlinedDescriptor Spacing(double value)
    {
        _inlined.HorizontalSpacing = value;
        _inlined.VerticalSpacing = value;
        return this;
    }

    /// <summary>Sets the horizontal gap between items in a row in points.</summary>
    public InlinedDescriptor HorizontalSpacing(double value)
    {
        _inlined.HorizontalSpacing = value;
        return this;
    }

    /// <summary>Sets the vertical gap between rows in points.</summary>
    public InlinedDescriptor VerticalSpacing(double value)
    {
        _inlined.VerticalSpacing = value;
        return this;
    }

    /// <summary>Aligns rows to the left (default).</summary>
    public InlinedDescriptor AlignLeft() => Align(HorizontalAlignment.Left);

    /// <summary>Centers rows within the available width.</summary>
    public InlinedDescriptor AlignCenter() => Align(HorizontalAlignment.Center);

    /// <summary>Aligns rows to the right.</summary>
    public InlinedDescriptor AlignRight() => Align(HorizontalAlignment.Right);

    /// <summary>Aligns items to the top of their row (default).</summary>
    public InlinedDescriptor AlignTop() => AlignVertical(VerticalAlignment.Top);

    /// <summary>Centers items vertically within their row.</summary>
    public InlinedDescriptor AlignMiddle() => AlignVertical(VerticalAlignment.Middle);

    /// <summary>Aligns items to the bottom of their row.</summary>
    public InlinedDescriptor AlignBottom() => AlignVertical(VerticalAlignment.Bottom);

    /// <summary>Adds an item to the flow.</summary>
    public ContainerDescriptor Item()
    {
        IElement placeholder = new EmptyElement();
        var index = _inlined.Items.Count;
        _inlined.Items.Add(placeholder);
        return new ContainerDescriptor(e => _inlined.Items[index] = e);
    }

    private InlinedDescriptor Align(HorizontalAlignment alignment)
    {
        _inlined.Alignment = alignment;
        return this;
    }

    private InlinedDescriptor AlignVertical(VerticalAlignment alignment)
    {
        _inlined.VerticalAlignment = alignment;
        return this;
    }
}
