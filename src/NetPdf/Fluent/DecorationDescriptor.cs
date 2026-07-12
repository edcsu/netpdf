using NetPdf.Layout;
using NetPdf.Layout.Elements;

namespace NetPdf.Fluent;

/// <summary>
/// Configures a decoration: a before slot repeated above and an after slot repeated below the
/// flowing content on every page the content spans.
/// </summary>
public sealed class DecorationDescriptor
{
    private Action<ContainerDescriptor>? _before;
    private Action<ContainerDescriptor>? _after;
    private Action<ContainerDescriptor>? _content;

    internal DecorationDescriptor()
    {
    }

    /// <summary>Describes the slot repeated above the content on every page.</summary>
    public void Before(Action<ContainerDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _before = configure;
    }

    /// <summary>Describes the flowing content between the repeated slots.</summary>
    public void Content(Action<ContainerDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _content = configure;
    }

    /// <summary>Describes the slot repeated below the content on every page.</summary>
    public void After(Action<ContainerDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _after = configure;
    }

    internal DecorationElement Build()
    {
        var element = new DecorationElement
        {
            Before = _before is { } before ? () => BuildSlot(before) : null,
            After = _after is { } after ? () => BuildSlot(after) : null,
        };
        if (_content is { } content)
            element.Content = BuildSlot(content);
        return element;
    }

    private static IElement BuildSlot(Action<ContainerDescriptor> configure)
    {
        IElement slot = new EmptyElement();
        configure(new ContainerDescriptor(e => slot = e));
        return slot;
    }
}
