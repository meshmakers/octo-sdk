using System.Net;
using System.Text;

namespace Sdk.ServiceClient.Tests.AssetRepositoryServices.Files;

/// <summary>
///     Message handler that records every request (incl. its body, read before the request is disposed) and
///     answers from a queue of canned responses, in order.
/// </summary>
internal sealed class RecordingHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses = new();

    private readonly Dictionary<int, TimeSpan> _delays = new();
    private int _answered;

    public List<RecordedRequest> Requests { get; } = [];

    public RecordingHandler Respond(HttpStatusCode status, string? body = null,
        string contentType = "application/json", Action<HttpResponseMessage>? configure = null)
    {
        _responses.Enqueue(_ =>
        {
            var response = new HttpResponseMessage(status);
            if (body != null)
            {
                response.Content = new StringContent(body, Encoding.UTF8);
                response.Content.Headers.ContentType =
                    System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);
            }

            configure?.Invoke(response);
            return response;
        });
        return this;
    }

    public RecordingHandler RespondBytes(byte[] bytes, Action<HttpResponseMessage> configure)
    {
        _responses.Enqueue(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };
            configure(response);
            return response;
        });
        return this;
    }

    /// <summary>Waits <paramref name="delay" /> (honouring cancellation) before answering 201 with <paramref name="body" />.</summary>
    public RecordingHandler RespondAfter(TimeSpan delay, string body)
    {
        _delays[_responses.Count] = delay;
        return Respond(HttpStatusCode.Created, body);
    }

    /// <summary>Answers a GraphQL request with <c>{"data": …}</c>.</summary>
    public RecordingHandler RespondData(string dataJson) =>
        Respond(HttpStatusCode.OK, "{\"data\":" + dataJson + "}");

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        byte[]? body = null;
        string? contentType = null;
        long? contentLength = null;
        if (request.Content != null)
        {
            contentType = request.Content.Headers.ContentType?.ToString();
            contentLength = request.Content.Headers.ContentLength;
            body = await request.Content.ReadAsByteArrayAsync(cancellationToken);
        }

        Requests.Add(new RecordedRequest(request.Method, request.RequestUri!,
            request.Headers.Authorization?.ToString(), contentType, contentLength, body));

        if (_responses.Count == 0)
        {
            throw new InvalidOperationException($"No response queued for {request.Method} {request.RequestUri}");
        }

        if (_delays.TryGetValue(_answered++, out var delay))
        {
            await Task.Delay(delay, cancellationToken);
        }

        return _responses.Dequeue()(request);
    }
}

internal sealed record RecordedRequest(
    HttpMethod Method,
    Uri Uri,
    string? Authorization,
    string? ContentType,
    long? ContentLength,
    byte[]? Body)
{
    public string BodyText => Body == null ? string.Empty : Encoding.UTF8.GetString(Body);

    /// <summary>The URI as sent on the wire (escapes preserved).</summary>
    public string Wire => Uri.AbsoluteUri;
}
