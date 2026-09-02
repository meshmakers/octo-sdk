using System.Net;
using System.Text;

namespace Sdk.ServiceClient.Tests.BotServices;

/// <summary>
///     A minimal in-process HTTP endpoint that records what it is sent and answers with just enough for
///     the bot services client to complete: a canned JSON body for the REST calls, and the create / head /
///     patch handshake of the tus resumable upload protocol for the two upload flows.
/// </summary>
/// <remarks>
///     <para>
///         The routing decisions of <c>BotServicesClient</c> — which URL a call uses, and whether the
///         tenant travels in the path or in a <c>tenantId</c> query parameter — are only observable on the
///         wire: <c>ServiceClient</c> constructs its own <c>RestClient</c>, so there is no message handler
///         to substitute, and the tus flows go out over a bare <c>HttpClient</c> entirely. Asserting on
///         <c>ServiceUri</c> would miss every call that deliberately does <i>not</i> use it, which after
///         AB#5060 is most of them. Binding a loopback listener is the smallest thing that proves the URL.
///     </para>
///     <para>
///         Used as an xUnit class fixture, i.e. one listener for the whole test class, with
///         <see cref="Reset" /> between tests. <see cref="HttpListener.Prefixes" />.<c>Add</c> costs about
///         five seconds on a macOS host (it is the prefix registration, not the request handling — a
///         plain console program pays the same), so an instance per test would add minutes to the suite.
///     </para>
/// </remarks>
public sealed class LoopbackHttpService : IDisposable
{
    /// <summary>The tus file id handed out by the fake upload sink.</summary>
    public const string TusFileId = "tus-file-1";

    /// <summary>
    ///     Canned body, shaped to satisfy every DTO the client deserialises here: <c>JobResponseDto</c>
    ///     (<c>jobId</c>) for the job starts and <c>JobDto</c> (<c>id</c>, <c>status</c>) for the status
    ///     read. One body keeps the fixture shareable across the whole class.
    /// </summary>
    private static readonly byte[] Payload =
        Encoding.UTF8.GetBytes("""{"jobId":"job-1","id":"job-1","status":"Succeeded"}""");

    private readonly HttpListener _listener = new();
    private readonly List<string> _requests = [];
    private readonly Lock _sync = new();

    public LoopbackHttpService()
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

    /// <summary>
    ///     Method, path and query of the requests received so far, in arrival order, e.g.
    ///     <c>POST /acme/v1/jobs/dump-repository?includeArchiveData=True</c>.
    /// </summary>
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

    /// <summary>Drops the recorded requests. Call at the start of every test sharing this fixture.</summary>
    public void Reset()
    {
        lock (_sync)
        {
            _requests.Clear();
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

            var request = context.Request;

            lock (_sync)
            {
                _requests.Add($"{request.HttpMethod} {request.Url?.PathAndQuery ?? string.Empty}");
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
        var request = context.Request;
        var response = context.Response;
        var path = request.Url?.AbsolutePath ?? string.Empty;

        // tus: create returns 201 plus the file location the client then PATCHes to.
        if (request.HttpMethod == "POST" && path.EndsWith("/tus-upload", StringComparison.Ordinal))
        {
            response.StatusCode = (int)HttpStatusCode.Created;
            response.AddHeader("Tus-Resumable", "1.0.0");
            response.AddHeader("Location", $"{BaseUrl.TrimEnd('/')}{path}/{TusFileId}");
            response.ContentLength64 = 0;
            response.Close();
            return;
        }

        // tus: the client asks for the current offset before it starts sending.
        if (request.HttpMethod == "HEAD")
        {
            response.StatusCode = (int)HttpStatusCode.OK;
            response.AddHeader("Tus-Resumable", "1.0.0");
            response.AddHeader("Upload-Offset", "0");
            response.AddHeader("Cache-Control", "no-store");
            response.ContentLength64 = 0;
            response.Close();
            return;
        }

        // tus: the upload itself is acknowledged with the new offset.
        if (request.HttpMethod == "PATCH")
        {
            long received = 0;
            var buffer = new byte[81920];
            int read;
            while ((read = await request.InputStream.ReadAsync(buffer)) > 0)
            {
                received += read;
            }

            response.StatusCode = (int)HttpStatusCode.NoContent;
            response.AddHeader("Tus-Resumable", "1.0.0");
            response.AddHeader("Upload-Offset", received.ToString());
            response.ContentLength64 = 0;
            response.Close();
            return;
        }

        response.StatusCode = (int)HttpStatusCode.OK;
        response.ContentType = "application/json";
        response.ContentLength64 = Payload.Length;
        await response.OutputStream.WriteAsync(Payload);
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
