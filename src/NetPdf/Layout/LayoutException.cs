namespace NetPdf.Layout;

/// <summary>
/// Thrown when the layout engine cannot make progress, e.g. content that does not fit
/// on an empty page or an element that partially renders without consuming any space.
/// </summary>
public sealed class LayoutException : Exception
{
    /// <summary>Creates the exception with a message describing the layout failure.</summary>
    public LayoutException(string message)
        : base(message)
    {
    }
}
