namespace NetPdf.Layout.Elements;

/// <summary>
/// Marks its child as a tagged-PDF structure element with a semantic role. On canvases
/// without tagging support (or documents not built with tagging enabled) it draws the
/// child unchanged.
/// </summary>
public sealed class SemanticElement : ContainerElement
{
    /// <summary>The structure role of the content.</summary>
    public SemanticRole Role { get; set; } = SemanticRole.Paragraph;

    /// <summary>Alternate text, primarily for <see cref="SemanticRole.Figure"/> content.</summary>
    public string? AltText { get; set; }

    /// <inheritdoc />
    public override void Draw(ICanvas canvas, Size availableSpace)
    {
        if (canvas is ITagCanvas tagger)
        {
            tagger.BeginMarkedContent(Role, AltText);
            Child.Draw(canvas, availableSpace);
            tagger.EndMarkedContent();
        }
        else
        {
            Child.Draw(canvas, availableSpace);
        }
    }
}
