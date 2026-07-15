using System.Diagnostics;
using System.Text;

namespace NetPdf.Previewer;

/// <summary>
/// Pushes documents to the netpdf-previewer desktop app over localhost HTTP
/// (<c>GET /ping</c>, <c>POST /document</c>, <c>POST /error</c>).
/// </summary>
internal sealed class PreviewerClient(int port, TimeSpan? launchTimeout = null)
{
    /// <summary>Default port the previewer app listens on.</summary>
    internal const int DefaultPort = 12500;

    private const string InstallHint =
        "The NetPdf previewer app is not running and could not be started. " +
        "Install it with: dotnet tool install -g NetPdfKit.Previewer.App";

    private static readonly HttpClient PingClient = new() { Timeout = TimeSpan.FromSeconds(2) };
    private static readonly HttpClient PushClient = new() { Timeout = TimeSpan.FromSeconds(30) };

    private readonly TimeSpan _launchTimeout = launchTimeout ?? TimeSpan.FromSeconds(10);
    private readonly string _baseUrl = $"http://127.0.0.1:{port}";

    /// <summary>Returns true when the previewer app answers on the configured port.</summary>
    internal bool Ping()
    {
        try
        {
            var body = PingClient.GetStringAsync($"{_baseUrl}/ping").GetAwaiter().GetResult();
            return body == "netpdf-previewer";
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }

    /// <summary>Pushes a generated PDF to the previewer app.</summary>
    internal void SendDocument(byte[] pdf) =>
        Post("/document", new ByteArrayContent(pdf));

    /// <summary>Reports a document-generation failure to the previewer app.</summary>
    internal void SendError(string message) =>
        Post("/error", new ByteArrayContent(Encoding.UTF8.GetBytes(message)));

    /// <summary>Attempts to launch the netpdf-previewer tool; false when it is not installed.</summary>
    internal bool TryLaunchApp()
    {
        try
        {
            var startInfo = new ProcessStartInfo("netpdf-previewer", $"--port {port}")
            {
                UseShellExecute = false,
            };
            return Process.Start(startInfo) is not null;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>
    /// Ensures the previewer app is reachable, launching it when needed.
    /// Throws <see cref="InvalidOperationException"/> with an install hint when it cannot be started.
    /// </summary>
    internal void EnsureRunning()
    {
        if (Ping())
        {
            return;
        }

        if (TryLaunchApp())
        {
            var deadline = DateTime.UtcNow + _launchTimeout;
            while (DateTime.UtcNow < deadline)
            {
                if (Ping())
                {
                    return;
                }

                Thread.Sleep(250);
            }
        }

        throw new InvalidOperationException(InstallHint);
    }

    private void Post(string path, HttpContent content)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}{path}")
        {
            Content = content,
        };
        request.Headers.Add("X-NetPdf-Previewer-Version", "1");
        using var response = PushClient.Send(request);
        response.EnsureSuccessStatusCode();
    }
}
