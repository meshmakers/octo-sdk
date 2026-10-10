using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Blueprints;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.System;

namespace Sdk.ServiceClient.Tests.AssetRepositoryServices.Blueprints;

/// <summary>
///     AB#6316: the SDK carries the blanking confirmation to the server (AB#6315 contract) and reads
///     the blanking report back, from the preview and from the apply result, while still working
///     against services that answer the apply with 204 and no body.
/// </summary>
public sealed class BlueprintUpdateBlankingClientTests : IDisposable
{
    private readonly WireService _service = new();

    public void Dispose() => _service.Dispose();

    [Fact]
    public async Task Preview_ReadsBlankedAttributes()
    {
        _service.Respond(200, """
            {"targetVersion":"Eda-1.2.0","blankedAttributes":[
              {"rtId":"65a000000000000000000001","ckTypeId":"System.Communication/Adapter","attributeName":"configuration",
               "reason":"SeedEmpty","currentSummary":"string (223 chars)","incomingSummary":"empty string","appliedOnUpdate":false}]}
            """);

        var preview = await NewClient().PreviewBlueprintUpdateAsync("acme",
            new BlueprintUpdateRequestDto { TargetVersion = "Eda-1.2.0", DryRun = true });

        Assert.Equal("POST /acme/v1/blueprints/updates/preview", _service.SingleRequest());
        var blanked = Assert.Single(preview.BlankedAttributes);
        Assert.Equal("65a000000000000000000001", blanked.RtId);
        Assert.Equal("configuration", blanked.AttributeName);
        Assert.Equal("SeedEmpty", blanked.Reason);
        Assert.Equal("string (223 chars)", blanked.CurrentSummary);
        Assert.Equal("empty string", blanked.IncomingSummary);
        Assert.False(blanked.AppliedOnUpdate);
    }

    [Fact]
    public async Task Preview_FromOlderService_HasEmptyBlankedAttributes()
    {
        _service.Respond(200, """{"targetVersion":"Eda-1.2.0","entitiesToUpdate":1}""");

        var preview = await NewClient().PreviewBlueprintUpdateAsync("acme",
            new BlueprintUpdateRequestDto { TargetVersion = "Eda-1.2.0", DryRun = true });

        Assert.Empty(preview.BlankedAttributes);
    }

    [Fact]
    public async Task Apply_SendsConfirmationsAsCamelCaseJson_AndReadsTheResult()
    {
        _service.Respond(200, """
            {"success":true,"entitiesUpdated":3,"warnings":["w"],"blankedAttributes":[
              {"rtId":"r1","attributeName":"a","reason":"SeedOmitted","appliedOnUpdate":true},
              {"rtId":"r2","attributeName":"b","reason":"SeedEmpty","appliedOnUpdate":false}]}
            """);

        var result = await NewClient().ApplyBlueprintUpdateAsync("acme", new BlueprintUpdateRequestDto
        {
            TargetVersion = "Eda-1.2.0",
            ConfirmedBlankings = [new BlueprintBlankingConfirmationDto { RtId = "r1", AttributeName = "a" }]
        });

        Assert.Equal("POST /acme/v1/blueprints/updates/apply", _service.SingleRequest());
        var body = JsonNode.Parse(_service.LastBody)!;
        Assert.False(body["allowBlanking"]!.GetValue<bool>());
        var confirmed = Assert.Single(body["confirmedBlankings"]!.AsArray());
        Assert.Equal("r1", confirmed!["rtId"]!.GetValue<string>());
        Assert.Equal("a", confirmed["attributeName"]!.GetValue<string>());

        Assert.True(result.Success);
        Assert.Equal(3, result.EntitiesUpdated);
        Assert.Equal(["w"], result.Warnings);
        Assert.Equal(["r1", "r2"], result.BlankedAttributes.Select(b => b.RtId));
        Assert.Equal([true, false], result.BlankedAttributes.Select(b => b.AppliedOnUpdate));
    }

    [Fact]
    public async Task Apply_AllowBlanking_IsSent()
    {
        _service.Respond(200, """{"success":true}""");

        await NewClient().ApplyBlueprintUpdateAsync("acme",
            new BlueprintUpdateRequestDto { TargetVersion = "Eda-1.2.0", AllowBlanking = true });

        Assert.True(JsonNode.Parse(_service.LastBody)!["allowBlanking"]!.GetValue<bool>());
    }

    [Fact]
    public async Task Apply_AgainstOldService_204WithoutBody_IsSuccessWithEmptyReport()
    {
        _service.Respond(204, string.Empty);

        var result = await NewClient().ApplyBlueprintUpdateAsync("acme",
            new BlueprintUpdateRequestDto { TargetVersion = "Eda-1.2.0" });

        Assert.True(result.Success);
        Assert.Empty(result.BlankedAttributes);
        Assert.Empty(result.Warnings);
    }

    private AssetServicesClient NewClient() =>
        new(new AssetServiceClientOptions { EndpointUri = _service.BaseUrl }, A.Fake<IAssetServiceClientAccessToken>());

    private sealed class WireService : IDisposable
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
                    _requests.Add($"{context.Request.HttpMethod} {context.Request.Url?.AbsolutePath}");
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
}
