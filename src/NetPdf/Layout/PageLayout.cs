namespace NetPdf.Layout;

/// <summary>
/// A page recipe for the render loop: geometry plus per-slot element factories. Header and
/// footer factories are invoked once per page because elements are stateful and single-use;
/// the content element is created once and paginates across pages.
/// </summary>
internal sealed class PageLayout
{
    /// <summary>The page width in points.</summary>
    public double PageWidth { get; init; } = 595;

    /// <summary>The page height in points.</summary>
    public double PageHeight { get; init; } = 842;

    /// <summary>The left margin in points.</summary>
    public double MarginLeft { get; init; } = 50;

    /// <summary>The top margin in points.</summary>
    public double MarginTop { get; init; } = 50;

    /// <summary>The right margin in points.</summary>
    public double MarginRight { get; init; } = 50;

    /// <summary>The bottom margin in points.</summary>
    public double MarginBottom { get; init; } = 50;

    /// <summary>Factory for the header element repeated at the top of every page, or null for none.</summary>
    public Func<IElement>? Header { get; init; }

    /// <summary>Factory for the content element that flows across pages.</summary>
    public required Func<IElement> Content { get; init; }

    /// <summary>Factory for the footer element repeated at the bottom of every page, or null for none.</summary>
    public Func<IElement>? Footer { get; init; }
}
