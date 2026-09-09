using System.Net;
using System.Net.Sockets;
using RedirectionVerifier;
using Xunit;

namespace GitHub.UnitTests;

public class RedirectTargetVerifierTests
{
    [Fact]
    public async Task WriteResultsAsyncReturnsTrueForValidUrl()
    {
        await using var server = new TestHttpServer(new Dictionary<string, HttpStatusCode>
        {
            ["/ok"] = HttpStatusCode.OK
        });

        string redirectionFilePath = await CreateRedirectionFileAsync($"{server.BaseUrl}/ok");
        try
        {
            using var writer = new StringWriter();
            bool result = await RedirectTargetVerifier.WriteResultsAsync(writer, redirectionFilePath);

            Assert.True(result);
            Assert.Equal(string.Empty, writer.ToString());
        }
        finally
        {
            File.Delete(redirectionFilePath);
        }
    }

    [Fact]
    public async Task WriteResultsAsyncReturnsFalseForInvalidUrl()
    {
        string redirectionFilePath = await CreateRedirectionFileAsync("not-a-valid-url");
        try
        {
            using var writer = new StringWriter();
            bool result = await RedirectTargetVerifier.WriteResultsAsync(writer, redirectionFilePath);

            Assert.False(result);
            Assert.Contains("Invalid 'redirect_url'", writer.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(redirectionFilePath);
        }
    }

    [Fact]
    public async Task WriteResultsAsyncReturnsFalseFor404Url()
    {
        await using var server = new TestHttpServer(new Dictionary<string, HttpStatusCode>
        {
            ["/missing"] = HttpStatusCode.NotFound
        });

        string redirectionFilePath = await CreateRedirectionFileAsync($"{server.BaseUrl}/missing");
        try
        {
            using var writer = new StringWriter();
            bool result = await RedirectTargetVerifier.WriteResultsAsync(writer, redirectionFilePath);

            Assert.False(result);
            Assert.Contains("returns 404", writer.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(redirectionFilePath);
        }
    }

    private static async Task<string> CreateRedirectionFileAsync(string redirectUrl)
    {
        string filePath = Path.Combine(Path.GetTempPath(), $"redirect-{Guid.NewGuid():N}.json");
        string content = $$"""
        {
          "redirections": [
            {
              "source_path": "docs/old.md",
              "redirect_url": "{{redirectUrl}}"
            }
          ]
        }
        """;

        await File.WriteAllTextAsync(filePath, content);
        return filePath;
    }

    private sealed class TestHttpServer : IAsyncDisposable
    {
        private readonly HttpListener _listener;
        private readonly Task _listenerTask;
        private readonly Dictionary<string, HttpStatusCode> _responses;

        public TestHttpServer(Dictionary<string, HttpStatusCode> responses)
        {
            _responses = responses;
            int port = GetFreePort();
            BaseUrl = $"http://127.0.0.1:{port}";

            _listener = new HttpListener();
            _listener.Prefixes.Add($"{BaseUrl}/");
            _listener.Start();

            _listenerTask = Task.Run(HandleRequestsAsync);
        }

        public string BaseUrl { get; }

        private async Task HandleRequestsAsync()
        {
            while (_listener.IsListening)
            {
                HttpListenerContext? context;
                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                string path = context.Request.Url?.AbsolutePath ?? "/";
                HttpStatusCode statusCode = _responses.TryGetValue(path, out HttpStatusCode configured)
                    ? configured
                    : HttpStatusCode.OK;

                context.Response.StatusCode = (int)statusCode;
                context.Response.ContentLength64 = 0;
                context.Response.Close();
            }
        }

        public async ValueTask DisposeAsync()
        {
            _listener.Stop();
            _listener.Close();
            await _listenerTask;
        }

        private static int GetFreePort()
        {
            using var tcpListener = new TcpListener(IPAddress.Loopback, 0);
            tcpListener.Start();
            int port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
            return port;
        }
    }
}
