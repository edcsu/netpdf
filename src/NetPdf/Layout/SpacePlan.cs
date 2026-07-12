namespace NetPdf.Layout;

/// <summary>How an element responded to the space offered during measurement.</summary>
public enum SpacePlanType
{
    /// <summary>Nothing fits in the offered space; the element must move to the next page.</summary>
    Wrap,
    /// <summary>Part of the content fits; the element will be measured and drawn again on the next page.</summary>
    PartialRender,
    /// <summary>All remaining content fits in the offered space.</summary>
    FullRender,
}

/// <summary>The result of measuring an element against an available space.</summary>
public readonly struct SpacePlan
{
    /// <summary>How the element responded to the offered space.</summary>
    public SpacePlanType Type { get; }

    /// <summary>The space the element will occupy when drawn (zero for <see cref="SpacePlanType.Wrap"/>).</summary>
    public Size Size { get; }

    private SpacePlan(SpacePlanType type, Size size)
    {
        Type = type;
        Size = size;
    }

    /// <summary>Nothing fits; the element must be offered a fresh page.</summary>
    public static SpacePlan Wrap() => new(SpacePlanType.Wrap, Size.Zero);

    /// <summary>Part of the content fits and occupies <paramref name="size"/>; the rest continues on the next page.</summary>
    public static SpacePlan PartialRender(Size size) => new(SpacePlanType.PartialRender, size);

    /// <summary>All remaining content fits and occupies <paramref name="size"/>.</summary>
    public static SpacePlan FullRender(Size size) => new(SpacePlanType.FullRender, size);
}
