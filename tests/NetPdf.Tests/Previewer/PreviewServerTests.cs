#if NET10_0_OR_GREATER
using System.Net;
using System.Net.Sockets;
using NetPdf.Previewer.App;
using Xunit;

namespace NetPdf.Tests.Previewer;

public class PreviewServerTests
{
    private static readonly HttpClient Http = new();

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static PreviewServer StartServer(out int port)
    {
        port = GetFreePort();
        var server = new PreviewServer(port);
        server.Start();
        return server;
    }

    [Fact]
    public async Task Ping_ReturnsIdentifier()
    {
        using var server = StartServer(out var port);

        var response = await Http.GetAsync($"http://127.0.0.1:{port}/ping");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("netpdf-previewer", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task PostDocument_RaisesDocumentReceivedWithBody()
    {
        using var server = StartServer(out var port);
        byte[]? received = null;
        server.DocumentReceived += bytes => received = bytes;
        var payload = new byte[] { 1, 2, 3, 4, 5 };

        var response = await Http.PostAsync(
            $"http://127.0.0.1:{port}/document", new ByteArrayContent(payload));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(payload, received);
    }

    [Fact]
    public async Task PostError_RaisesErrorReceivedWithText()
    {
        using var server = StartServer(out var port);
        string? received = null;
        server.ErrorReceived += text => received = text;

        var response = await Http.PostAsync(
            $"http://127.0.0.1:{port}/error", new StringContent("layout overflow"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("layout overflow", received);
    }

    [Fact]
    public async Task UnknownPath_Returns404()
    {
        using var server = StartServer(out var port);

        var response = await Http.GetAsync($"http://127.0.0.1:{port}/bogus");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
#endif
