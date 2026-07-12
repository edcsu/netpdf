using NetPdf.Layout;
using NetPdf.Layout.Elements;

namespace NetPdf.Fluent;

/// <summary>
/// Configures one page section: geometry plus header, content, and footer slots. The slot
/// lambdas are stored and replayed per page (header/footer) and per render pass, so a fresh
/// element tree is built each time.
/// </summary>
public sealed class PageDescriptor
{
    private double _width = 595;
    private double _height = 842;
    private double _marginLeft = 50;
    private double _marginTop = 50;
    private double _marginRight = 50;
    private double _marginBottom = 50;
    private Action<ContainerDescriptor>? _header;
    private Action<ContainerDescriptor>? _content;
    private Action<ContainerDescriptor>? _footer;

    internal PageDescriptor()
    {
    }

    /// <summary>Sets the page size in points. Defaults to A4.</summary>
    public PageDescriptor Size(double width, double height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        _width = width;
        _height = height;
        return this;
    }

    /// <summary>Sets the page size in points from a preset (see <see cref="PageSizes"/>).</summary>
    public PageDescriptor Size(Layout.Size size) => Size(size.Width, size.Height);

    /// <summary>Sets the same margin on all sides in points. Defaults to 50.</summary>
    public PageDescriptor Margin(double all) => Margin(all, all, all, all);

    /// <summary>Sets individual margins per side in points.</summary>
    public PageDescriptor Margin(double left, double top, double right, double bottom)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(left);
        ArgumentOutOfRangeException.ThrowIfNegative(top);
        ArgumentOutOfRangeException.ThrowIfNegative(right);
        ArgumentOutOfRangeException.ThrowIfNegative(bottom);
        _marginLeft = left;
        _marginTop = top;
        _marginRight = right;
        _marginBottom = bottom;
        return this;
    }

    /// <summary>Configures the header repeated at the top of every page.</summary>
    public PageDescriptor Header(Action<ContainerDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _header = configure;
        return this;
    }

    /// <summary>Configures the content that flows across pages.</summary>
    public PageDescriptor Content(Action<ContainerDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _content = configure;
        return this;
    }

    /// <summary>Configures the footer repeated at the bottom of every page.</summary>
    public PageDescriptor Footer(Action<ContainerDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _footer = configure;
        return this;
    }

    internal PageLayout ToLayout() => new()
    {
        PageWidth = _width,
        PageHeight = _height,
        MarginLeft = _marginLeft,
        MarginTop = _marginTop,
        MarginRight = _marginRight,
        MarginBottom = _marginBottom,
        Header = _header is null ? null : () => BuildSlot(_header),
        Content = () => BuildSlot(_content),
        Footer = _footer is null ? null : () => BuildSlot(_footer),
    };

    private static IElement BuildSlot(Action<ContainerDescriptor>? configure)
    {
        IElement element = new EmptyElement();
        configure?.Invoke(new ContainerDescriptor(e => element = e));
        return element;
    }
}
