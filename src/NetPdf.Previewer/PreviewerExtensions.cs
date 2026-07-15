using NetPdf.Fluent;

namespace NetPdf.Previewer;

/// <summary>Live-preview extensions for <see cref="DocumentBuilder"/>.</summary>
public static class PreviewerExtensions
{
    /// <summary>
    /// Generates the document and shows it in the netpdf-previewer desktop app,
    /// launching the app when it is not already running. Run your program under
    /// <c>dotnet watch run</c> to re-render the preview on every code change.
    /// If generation fails, the error is shown in the previewer and rethrown.
    /// </summary>
    /// <param name="builder">The document to preview.</param>
    /// <param name="port">Port the previewer app listens on.</param>
    public static void ShowInPreviewer(this DocumentBuilder builder, int port = PreviewerClient.DefaultPort)
    {
        var client = new PreviewerClient(port);
        client.EnsureRunning();

        byte[] pdf;
        try
        {
            pdf = builder.ToBytes();
        }
        catch (Exception ex)
        {
            client.SendError(ex.ToString());
            throw;
        }

        client.SendDocument(pdf);
    }
}
