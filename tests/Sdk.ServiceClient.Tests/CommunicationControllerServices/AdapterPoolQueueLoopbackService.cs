using System.Net;
using System.Text;

namespace Sdk.ServiceClient.Tests.CommunicationControllerServices;

/// <summary>
///     A minimal in-process HTTP endpoint for the adapter pool queue endpoints, with a programmable
///     answer per test.
/// </summary>
/// <remarks>
///     <para>
///         The interesting behaviour of <c>GetAdapterPoolQueueAsync</c> / <c>CancelQueuedExecutionAsync</c>
///         is what the client does with a <b>status code</b> — 204, 409 and 404 mean three different
///         things and only one of them is a failure — and that is only observable on the wire:
///         <c>ServiceClient</c> builds its own <c>RestClient</c>, so there is no message handler to
///         substitute.
///     </para>
///     <para>
///         Used as an xUnit class fixture — one listener for the whole class, <see cref="Reset" />
///         between tests. <see cref="HttpListener.Prefixes" />.<c>Add</c> costs seconds on a macOS host,
///         so an instance per test would dominate the suite (same reasoning as
///         <c>BotServices/LoopbackHttpService</c>).
///     </para>
/// </remarks>
public sealed class AdapterPoolQueueLoopbackService : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly List<string> _requests = [];
    private readonly Lock _sync = new();
    private string _body = "[]";
    private HttpStatusCode _status = HttpStatusCode.OK;

    public AdapterPoolQueueLoopbackService()
    {
        var port = FreeTcpPort();
        BaseUrl = $"http://127.0.0.1:{port}/";
        _listener.Prefixes.Add(BaseUrl);
        _listener.Start();

        _ = Task.Run(ServeAsync);
    }

    /// <summary>Base address to configure the client under test with.</summary>
    public string BaseUrl { get; }

    public void Dispose()
    {
        _listener.Abort();
    }

    /// <summary>Method, path and query of the requests received so far, in arrival order.</summary>
    public IReadOnlyList<string> Requests
    {
        get
        {
            lock (_sync)
            {
                return _requests.ToArray();
            }
        }
    }

    /// <summary>Drops recorded requests and restores the default 200/empty-array answer.</summary>
    public void Reset()
    {
        lock (_sync)
        {
            _requests.Clear();
            _status = HttpStatusCode.OK;
            _body = "[]";
        }
    }

    /// <summary>Sets the status code and body the next requests are answered with.</summary>
    public void Respond(HttpStatusCode status, string body = "")
    {
        lock (_sync)
        {
            _status = status;
            _body = body;
        }
    }

    /// <summary>Returns the single request received, failing the test when there is not exactly one.</summary>
    public string SingleRequest()
    {
        var requests = Requests;
        Assert.Single(requests);
        return requests[0];
    }

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
                // Listener closed while waiting — that is how the loop ends.
                return;
            }

            lock (_sync)
            {
                _requests.Add($"{context.Request.HttpMethod} {context.Request.Url?.PathAndQuery ?? string.Empty}");
            }

            try
            {
                await RespondAsync(context);
            }
            catch (Exception)
            {
                // A client that gave up mid-response must not take the listener down.
            }
        }
    }

    private async Task RespondAsync(HttpListenerContext context)
    {
        HttpStatusCode status;
        string body;
        lock (_sync)
        {
            status = _status;
            body = _body;
        }

        var response = context.Response;
        response.StatusCode = (int)status;

        if (string.IsNullOrEmpty(body))
        {
            response.ContentLength64 = 0;
            response.Close();
            return;
        }

        var payload = Encoding.UTF8.GetBytes(body);
        response.ContentType = "application/json";
        response.ContentLength64 = payload.Length;
        await response.OutputStream.WriteAsync(payload);
        response.Close();
    }

    private static int FreeTcpPort()
    {
        using var socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork,
            System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)socket.LocalEndPoint!).Port;
    }
}
