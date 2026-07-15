using System.Net;
using System.Text;

namespace NetPdf.Previewer.App;

/// <summary>
/// Localhost HTTP listener that receives documents and error reports pushed by
/// the NetPdfKit.Previewer client (<c>GET /ping</c>, <c>POST /document</c>, <c>POST /error</c>).
/// </summary>
public sealed class PreviewServer(int port) : IDisposable
{
    private readonly HttpListener _listener = new();
    private bool _started;

    /// <summary>Port the server listens on.</summary>
    public int Port { get; } = port;

    /// <summary>Raised with the raw PDF bytes when a client pushes a document.</summary>
    public event Action<byte[]>? DocumentReceived;

    /// <summary>Raised with the error text when a client reports a generation failure.</summary>
    public event Action<string>? ErrorReceived;

    /// <summary>Starts listening and processing requests in the background.</summary>
    public void Start()
    {
        _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
        _listener.Start();
        _started = true;
        _ = Task.Run(AcceptLoopAsync);
    }

    private async Task AcceptLoopAsync()
    {
        while (_listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException)
            {
                return; // listener disposed
            }

            try
            {
                await HandleAsync(context).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException)
            {
                // Client disconnected mid-request; keep serving.
            }
        }
    }

    private async Task HandleAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        switch (request.HttpMethod, request.Url?.AbsolutePath)
        {
            case ("GET", "/ping"):
                var identifier = Encoding.UTF8.GetBytes("netpdf-previewer");
                response.StatusCode = 200;
                await response.OutputStream.WriteAsync(identifier).ConfigureAwait(false);
                break;

            case ("POST", "/document"):
                DocumentReceived?.Invoke(await ReadBodyAsync(request).ConfigureAwait(false));
                response.StatusCode = 200;
                break;

            case ("POST", "/error"):
                var body = await ReadBodyAsync(request).ConfigureAwait(false);
                ErrorReceived?.Invoke(Encoding.UTF8.GetString(body));
                response.StatusCode = 200;
                break;

            default:
                response.StatusCode = 404;
                break;
        }

        response.Close();
    }

    private static async Task<byte[]> ReadBodyAsync(HttpListenerRequest request)
    {
        using var buffer = new MemoryStream();
        await request.InputStream.CopyToAsync(buffer).ConfigureAwait(false);
        return buffer.ToArray();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_started)
        {
            _listener.Stop();
        }

        _listener.Close();
    }
}
