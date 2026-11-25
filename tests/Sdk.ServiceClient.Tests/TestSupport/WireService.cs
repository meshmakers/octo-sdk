using System.Net;
using System.Text;

namespace Sdk.ServiceClient.Tests.TestSupport;

/// <summary>A tiny HTTP listener that answers every request with one canned response and records the requests.</summary>
internal sealed class WireService : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly List<string> _requests = [];
    private (int Status, string Body) _response = (200, "{}");

    public WireService()
    {
        using var socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork,
            System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        BaseUrl = $"http://127.0.0.1:{((IPEndPoint)socket.LocalEndPoint!).Port}/";
        socket.Close();
        _listener.Prefixes.Add(BaseUrl);
        _listener.Start();
        _ = Task.Run(ServeAsync);
    }

    public string BaseUrl { get; }
    public string LastBody { get; private set; } = string.Empty;

    public void Respond(int status, string body) => _response = (status, body);

    public string SingleRequest()
    {
        lock (_requests)
        {
            return Assert.Single(_requests);
        }
    }

    public void Dispose() => _listener.Abort();

    private async Task ServeAsync()
    {
        while (_listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (Exception)
            {
                return;
            }

            using var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
            LastBody = await reader.ReadToEndAsync();
            lock (_requests)
            {
                _requests.Add($"{context.Request.HttpMethod} {context.Request.Url?.PathAndQuery}");
            }

            var (status, body) = _response;
            context.Response.StatusCode = status;
            if (status != 204)
            {
                var bytes = Encoding.UTF8.GetBytes(body);
                context.Response.ContentType = "application/json";
                context.Response.ContentLength64 = bytes.Length;
                await context.Response.OutputStream.WriteAsync(bytes);
            }

            context.Response.Close();
        }
    }
}
