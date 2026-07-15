using System.Net;
using System.Net.Sockets;
using System.Text;
using NetPdf.Fluent;
using NetPdf.Previewer;
using Xunit;

namespace NetPdf.Tests.Previewer;

public class PreviewerClientTests
{
    /// <summary>Minimal HTTP stub that records requests and answers /ping like the previewer app.</summary>
    private sealed class StubPreviewer : IDisposable
    {
        private readonly HttpListener _listener = new();

        public int Port { get; }
        public List<(string Method, string Path, string? VersionHeader, byte[] Body)> Requests { get; } = [];

        public StubPreviewer()
        {
            Port = GetFreePort();
            _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
            _listener.Start();
            _ = Task.Run(async () =>
            {
                while (_listener.IsListening)
                {
                    HttpListenerContext context;
                    try
                    {
                        context = await _listener.GetContextAsync();
                    }
                    catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException)
                    {
                        return;
                    }

                    using var buffer = new MemoryStream();
                    await context.Request.InputStream.CopyToAsync(buffer);
                    lock (Requests)
                    {
                        Requests.Add((
                            context.Request.HttpMethod,
                            context.Request.Url!.AbsolutePath,
                            context.Request.Headers["X-NetPdf-Previewer-Version"],
                            buffer.ToArray()));
                    }

                    if (context.Request.Url!.AbsolutePath == "/ping")
                    {
                        var identifier = Encoding.UTF8.GetBytes("netpdf-previewer");
                        await context.Response.OutputStream.WriteAsync(identifier);
                    }

                    context.Response.StatusCode = 200;
                    context.Response.Close();
                }
            });
        }

        public void Dispose() => _listener.Close();

        private static int GetFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }

    [Fact]
    public void SendDocument_PostsBytesWithVersionHeader()
    {
        using var stub = new StubPreviewer();
        var client = new PreviewerClient(stub.Port);
        var payload = new byte[] { 9, 8, 7 };

        client.SendDocument(payload);

        var request = Assert.Single(stub.Requests);
        Assert.Equal("POST", request.Method);
        Assert.Equal("/document", request.Path);
        Assert.Equal("1", request.VersionHeader);
        Assert.Equal(payload, request.Body);
    }

    [Fact]
    public void SendError_PostsTextToErrorEndpoint()
    {
        using var stub = new StubPreviewer();
        var client = new PreviewerClient(stub.Port);

        client.SendError("boom");

        var request = Assert.Single(stub.Requests);
        Assert.Equal("POST", request.Method);
        Assert.Equal("/error", request.Path);
        Assert.Equal("boom", Encoding.UTF8.GetString(request.Body));
    }

    [Fact]
    public void Ping_TrueWhenPreviewerAnswers()
    {
        using var stub = new StubPreviewer();
        var client = new PreviewerClient(stub.Port);

        Assert.True(client.Ping());
    }

    [Fact]
    public void Ping_FalseWhenNothingListens()
    {
        using var stub = new StubPreviewer();
        var deadPort = stub.Port;
        stub.Dispose();
        var client = new PreviewerClient(deadPort);

        Assert.False(client.Ping());
    }

    [Fact]
    public void ShowInPreviewer_GenerationFailure_SendsErrorAndRethrows()
    {
        using var stub = new StubPreviewer();

        // Margins larger than the page make rendering fail inside ToBytes().
        var builder = Document.Create(c => c.Page(p =>
        {
            p.Size(100, 100);
            p.Margin(200);
        }));

        var ex = Assert.Throws<NetPdf.Layout.LayoutException>(
            () => builder.ShowInPreviewer(stub.Port));

        lock (stub.Requests)
        {
            var error = stub.Requests.Single(r => r.Path == "/error");
            Assert.Contains(ex.Message, Encoding.UTF8.GetString(error.Body));
        }
    }

    [Fact]
    public void EnsureRunning_NoListenerAndNoTool_ThrowsWithInstallHint()
    {
        using var stub = new StubPreviewer();
        var deadPort = stub.Port;
        stub.Dispose();
        var client = new PreviewerClient(deadPort, launchTimeout: TimeSpan.FromMilliseconds(200));

        var ex = Assert.Throws<InvalidOperationException>(client.EnsureRunning);

        Assert.Contains("dotnet tool install", ex.Message);
    }
}
