using System.Text.Json;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Contracts.MessageObjects;

namespace Communication.Contracts.Tests.MessageObjects;

/// <summary>
///     AB#7458 — the method-invocation marker is an additive, nullable wire member. These tests pin
///     the compatibility in both directions for System.Text.Json (MassTransit and SignalR both use
///     it), with default options and with the web (camelCase) options.
/// </summary>
public class ExecutePipelineMethodInvocationTests
{
    private static readonly Guid InvocationId = Guid.Parse("3f2b8c1e-5a47-4d0e-9b36-7c1a2e4d8f90");
    private const string MarkerMethodId = "Marker-1/Marker-1/MARKER_METHOD_ID";
    private const string MarkerTypeId = "Marker-1/MARKER_TYPE";
    private const string MarkerPipelineRtId = "665f0000000000000000ee77";

    public static TheoryData<string> OptionNames => new() { "default", "web" };

    private static JsonSerializerOptions Options(string name) =>
        name == "web" ? new JsonSerializerOptions(JsonSerializerDefaults.Web) : new JsonSerializerOptions();

    private static ExecutePipelineMethodInvocation AMarker() => new()
    {
        MethodId = MarkerMethodId,
        InvocationId = InvocationId,
        CkTypeId = MarkerTypeId,
        PipelineRtId = MarkerPipelineRtId
    };

    private static LeaseDto ALease(ExecutePipelineMethodInvocation? marker) => new()
    {
        LeaseId = "lease-1",
        TenantId = "borrower",
        AdapterPoolTenantId = "lender",
        ClientId = "client",
        PipelineRtId = MarkerPipelineRtId,
        MethodInvocation = marker,
        GrantedAtUtc = new DateTime(2026, 10, 25, 10, 0, 0, DateTimeKind.Utc),
        ExpiresAtUtc = new DateTime(2026, 10, 25, 10, 5, 0, DateTimeKind.Utc)
    };

    private static void AssertMarker(ExecutePipelineMethodInvocation? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(MarkerMethodId, actual!.MethodId);
        Assert.Equal(InvocationId, actual.InvocationId);
        Assert.Equal(MarkerTypeId, actual.CkTypeId);
        Assert.Equal(MarkerPipelineRtId, actual.PipelineRtId);
    }

    [Theory]
    [MemberData(nameof(OptionNames))]
    public void Request_RoundTripsTheMarker(string options)
    {
        var request = new ExecutePipelineRequest("t1", "{}") { MethodInvocation = AMarker() };
        var o = Options(options);

        var round = JsonSerializer.Deserialize<ExecutePipelineRequest>(JsonSerializer.Serialize(request, o), o);

        Assert.NotNull(round);
        AssertMarker(round!.MethodInvocation);
        Assert.Equal("t1", round.TenantId);
    }

    [Theory]
    [MemberData(nameof(OptionNames))]
    public void Lease_RoundTripsTheMarker(string options)
    {
        var o = Options(options);

        var round = JsonSerializer.Deserialize<LeaseDto>(JsonSerializer.Serialize(ALease(AMarker()), o), o);

        Assert.NotNull(round);
        AssertMarker(round!.MethodInvocation);
    }

    [Theory]
    [MemberData(nameof(OptionNames))]
    public void Request_PreviousContractPayload_YieldsNullMarker(string options)
    {
        var o = Options(options);
        var json = options == "web"
            ? "{\"tenantId\":\"t1\",\"pipelineInput\":\"{}\",\"isDryRun\":false}"
            : "{\"TenantId\":\"t1\",\"PipelineInput\":\"{}\",\"IsDryRun\":false}";

        var request = JsonSerializer.Deserialize<ExecutePipelineRequest>(json, o);

        Assert.NotNull(request);
        Assert.Null(request!.MethodInvocation);
        Assert.Equal("t1", request.TenantId);
    }

    [Theory]
    [MemberData(nameof(OptionNames))]
    public void Lease_PreviousContractPayload_YieldsNullMarker(string options)
    {
        var o = Options(options);
        var legacy = ALease(AMarker());
        // Previous contract: serialize the current shape, then strip the member.
        var node = System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(legacy, o))!.AsObject();
        Assert.True(node.Remove(options == "web" ? "methodInvocation" : "MethodInvocation"));

        var lease = JsonSerializer.Deserialize<LeaseDto>(node.ToJsonString(), o);

        Assert.NotNull(lease);
        Assert.Null(lease!.MethodInvocation);
        Assert.Equal("lease-1", lease.LeaseId);
    }

    [Theory]
    [MemberData(nameof(OptionNames))]
    public void Request_NullMarker_RoundTripsAsNull(string options)
    {
        var o = Options(options);

        var round = JsonSerializer.Deserialize<ExecutePipelineRequest>(
            JsonSerializer.Serialize(new ExecutePipelineRequest("t1", null), o), o);

        Assert.Null(round!.MethodInvocation);
    }

    // Old-contract stand-ins: the shape of the types before AB#7458, without the member.
    private sealed record OldExecutePipelineRequest(string TenantId, string? PipelineInput)
    {
        public bool IsDryRun { get; init; }
    }

    private sealed record OldLease
    {
        public string LeaseId { get; init; } = string.Empty;
        public string TenantId { get; init; } = string.Empty;
    }

    [Theory]
    [MemberData(nameof(OptionNames))]
    public void NewRequestPayload_DeserializesIntoOldShape_IgnoringTheUnknownMember(string options)
    {
        var o = Options(options);
        var json = JsonSerializer.Serialize(new ExecutePipelineRequest("t1", "in") { MethodInvocation = AMarker() }, o);
        Assert.Contains(MarkerMethodId, json, StringComparison.Ordinal);

        var old = JsonSerializer.Deserialize<OldExecutePipelineRequest>(json, o);

        Assert.NotNull(old);
        Assert.Equal("t1", old!.TenantId);
        Assert.Equal("in", old.PipelineInput);
    }

    [Theory]
    [MemberData(nameof(OptionNames))]
    public void NewLeasePayload_DeserializesIntoOldShape_IgnoringTheUnknownMember(string options)
    {
        var o = Options(options);
        var json = JsonSerializer.Serialize(ALease(AMarker()), o);
        Assert.Contains(MarkerMethodId, json, StringComparison.Ordinal);

        var old = JsonSerializer.Deserialize<OldLease>(json, o);

        Assert.NotNull(old);
        Assert.Equal("lease-1", old!.LeaseId);
        Assert.Equal("borrower", old.TenantId);
    }

    /// <summary>The marker is not rendered by <see cref="LeaseDto.ToString" />.</summary>
    [Fact]
    public void LeaseToString_DoesNotRenderTheMarker()
    {
        var rendered = ALease(AMarker()).ToString();

        Assert.DoesNotContain(MarkerMethodId, rendered, StringComparison.Ordinal);
        Assert.DoesNotContain(MarkerTypeId, rendered, StringComparison.Ordinal);
        Assert.DoesNotContain(InvocationId.ToString(), rendered, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("lease-1", rendered, StringComparison.Ordinal);
    }
}
