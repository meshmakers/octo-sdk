using System.Text.Json;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;

namespace Communication.Contracts.Tests.DataTransferObjects;

/// <summary>
///     AB#4924 increment 6 — the wire shape of a lease.
/// </summary>
/// <remarks>
///     🔴 The reason this file exists is <see cref="LeaseDto.ClientSecret" />. Concept §8 Q6 put the
///     borrower's own <c>PipelineServiceAccount</c> credential on the lease, which means a client
///     secret now travels over the hub as well as over the deploy path. The AB#5027 deploy path is
///     guarded by <c>DeployWorkloadAsync_NeverWritesTheClientSecretToAnyLogTarget</c>; this is the
///     half of that guard that belongs to the contract rather than to a service — a record's
///     generated <c>ToString</c> prints every property, so the first person to write
///     <c>logger.LogDebug("… {Lease}", lease)</c> would put the secret in the log without noticing.
/// </remarks>
public class LeaseDtoTests
{
    private const string Secret = "sJ8k2p-QmZ4x7vNb1LcT0aRwEyUiOpAsDfGhJkLzXcVbNm";

    // AB#4924 §9.9 / D4: the enlarged lease carries the borrower's payload and the pipeline's own
    // configuration entries, which since AB#5027 hold a service-account credential of their own.
    // Two distinct markers so a test can say WHICH half leaked.
    private const string InputPayload = "{\"invoiceNumber\":\"BORROWER-PRIVATE-4711\"}";
    private const string ConfigurationSecret = "cfgSecret-9F2a7Lq0ZxBv3TnE8Rd1Yh6Ks4Mw5Pu2";

    private static LeaseDto ALease() => new()
    {
        LeaseId = "lease-1",
        TenantId = "borrower",
        PoolTenantId = "lender",
        PoolRtId = "665f0000000000000000ee21",
        AdapterRtId = "665f0000000000000000ee22",
        AdapterCkTypeId = "System.Communication/Adapter",
        ExecutionId = "exec-1",
        ClientId = "octo-pipeline-sa-borrower",
        ClientSecret = Secret,
        PipelineRtId = "665f0000000000000000ee23",
        PipelineInput = InputPayload,
        Pipeline = APipelineConfiguration(),
        GrantedAtUtc = new DateTime(2026, 9, 14, 10, 0, 0, DateTimeKind.Utc),
        ExpiresAtUtc = new DateTime(2026, 9, 14, 10, 5, 0, DateTimeKind.Utc)
    };

    private static PipelineConfigurationDto APipelineConfiguration() => new(
        new OctoObjectId("665f0000000000000000ee24"),
        new RtEntityId("System.Communication/Pipeline", new OctoObjectId("665f0000000000000000ee23")),
        false,
        "triggers:\n  - node: FromExecutePipelineCommand@1\n",
        [
            new ConfigurationDto(new OctoObjectId("665f0000000000000000ee25"),
                "System.Communication/ServiceAccountConfiguration", "adapter-service-account",
                "{\"attributes\":{\"clientSecret\":\"" + ConfigurationSecret + "\"}}")
        ]);

    /// <summary>
    ///     🔴 Neither the secret verbatim nor a prefix of it. A truncated secret is still secret
    ///     material, and "we only log the first eight characters" is how these leaks are argued into
    ///     existence.
    /// </summary>
    [Fact]
    public void ToString_NeverRendersTheClientSecret()
    {
        var rendered = ALease().ToString();

        Assert.DoesNotContain(Secret, rendered, StringComparison.Ordinal);
        Assert.DoesNotContain(Secret[..8], rendered, StringComparison.Ordinal);
    }

    /// <summary>
    ///     The rendering is only worth anything if it still identifies the lease — otherwise the
    ///     obvious "fix" for a useless log line is to print the object some other way.
    /// </summary>
    [Fact]
    public void ToString_StillIdentifiesTheLease()
    {
        var rendered = ALease().ToString();

        Assert.Contains("lease-1", rendered, StringComparison.Ordinal);
        Assert.Contains("borrower", rendered, StringComparison.Ordinal);
        Assert.Contains("665f0000000000000000ee21", rendered, StringComparison.Ordinal);
        Assert.Contains("octo-pipeline-sa-borrower", rendered, StringComparison.Ordinal);
    }

    /// <summary>
    ///     The secret must survive serialization — the member cannot log in without it. A "safe" DTO
    ///     that dropped the value on the wire would fail the lease, not protect it.
    /// </summary>
    [Fact]
    public void RoundTripsThroughJson_SecretIncluded()
    {
        var lease = ALease();

        var round = JsonSerializer.Deserialize<LeaseDto>(JsonSerializer.Serialize(lease));

        Assert.NotNull(round);
        Assert.Equal(lease, round);
        Assert.Equal(Secret, round!.ClientSecret);
        Assert.Equal("lender", round.PoolTenantId);
        Assert.Equal(lease.ExpiresAtUtc, round.ExpiresAtUtc);
    }

    /// <summary>
    ///     The borrower tenant and the lending tenant are separate fields and must stay so: one is
    ///     who the work belongs to, the other is who owns the process. Collapsing them is the
    ///     mistake concept §4b had to correct in prose.
    /// </summary>
    [Fact]
    public void BorrowerAndLenderAreDistinctFields()
    {
        var lease = ALease();

        Assert.NotEqual(lease.TenantId, lease.PoolTenantId);
    }

    /// <summary>
    ///     A release carries no credential back. The credential travelled one way; echoing it would
    ///     add a second copy to every log and store that touches a release.
    /// </summary>
    [Fact]
    public void LeaseResult_CarriesNoCredentialMaterial()
    {
        var properties = typeof(LeaseResultDto).GetProperties().Select(p => p.Name).ToList();

        Assert.DoesNotContain("ClientSecret", properties);
        Assert.DoesNotContain("ClientId", properties);
    }

    /// <summary>
    ///     🔴 AB#4924 §9.9 / D4 — the lease grew two work-carrying members, and both are payload of the
    ///     borrowing tenant. Neither may appear in the rendering, for the same reason the secret may
    ///     not: the process serves a different tenant a second later.
    /// </summary>
    [Fact]
    public void ToString_RendersNeitherTheInputNorThePipelineConfiguration()
    {
        var rendered = ALease().ToString();

        Assert.DoesNotContain(InputPayload, rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("BORROWER-PRIVATE-4711", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain(ConfigurationSecret, rendered, StringComparison.Ordinal);
        Assert.DoesNotContain(ConfigurationSecret[..8], rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("FromExecutePipelineCommand", rendered, StringComparison.Ordinal);
    }

    /// <summary>
    ///     The rendering still has to say which work the lease was for — otherwise a log line about a
    ///     lease cannot be joined to the execution it served.
    /// </summary>
    [Fact]
    public void ToString_NamesThePipelineAndTheExecution()
    {
        var rendered = ALease().ToString();

        Assert.Contains("665f0000000000000000ee23", rendered, StringComparison.Ordinal);
        Assert.Contains("exec-1", rendered, StringComparison.Ordinal);
    }

    /// <summary>
    ///     A lease that carries no work says so rather than printing an empty rtId that reads like a
    ///     truncated one.
    /// </summary>
    [Fact]
    public void ToString_SaysSoWhenTheLeaseCarriesNoWork()
    {
        var rendered = (ALease() with { PipelineRtId = string.Empty }).ToString();

        Assert.Contains("<none>", rendered, StringComparison.Ordinal);
    }

    /// <summary>
    ///     The work has to survive the wire, exactly like the secret does: a lease whose input or
    ///     pipeline configuration was dropped in transit would run the wrong thing, not nothing.
    /// </summary>
    [Fact]
    public void RoundTripsThroughJson_WorkIncluded()
    {
        var lease = ALease();

        var round = JsonSerializer.Deserialize<LeaseDto>(JsonSerializer.Serialize(lease));

        Assert.NotNull(round);
        Assert.Equal("665f0000000000000000ee23", round!.PipelineRtId);
        Assert.Equal(InputPayload, round.PipelineInput);
        Assert.NotNull(round.Pipeline);
        Assert.Equal(lease.Pipeline!.NodeConfiguration, round.Pipeline!.NodeConfiguration);
        Assert.Equal(lease.Pipeline.PipelineRtEntityId, round.Pipeline.PipelineRtEntityId);
        Assert.Contains(ConfigurationSecret,
            round.Pipeline.Configurations.Single().ConfigurationValue, StringComparison.Ordinal);
    }

    /// <summary>
    ///     A lease with no work item behind it stays legal — increment 6's hand-driven
    ///     <c>POST {tenantId}/v1/adapterPool/{id}/lease</c> grants exactly that, and it is what the
    ///     wire contract was verified with one increment before the scheduler existed.
    /// </summary>
    [Fact]
    public void ALeaseWithoutWorkIsStillAValidLease()
    {
        var lease = new LeaseDto { LeaseId = "lease-2", TenantId = "borrower" };

        Assert.Equal(string.Empty, lease.PipelineRtId);
        Assert.Null(lease.PipelineInput);
        Assert.Null(lease.Pipeline);
    }

    /// <summary>
    ///     The release now carries the pipeline's output back — it is the only way a leased
    ///     execution's <c>OutputData</c> can reach its entity (the member has no adapter-hub
    ///     connection to report an execution end on). It still carries no credential material.
    /// </summary>
    [Fact]
    public void LeaseResult_CarriesTheOutputButStillNoCredential()
    {
        var result = new LeaseResultDto { LeaseId = "lease-1", OutputData = "{\"total\":42}" };

        Assert.Equal("{\"total\":42}", result.OutputData);
        var properties = typeof(LeaseResultDto).GetProperties().Select(p => p.Name).ToList();
        Assert.DoesNotContain("ClientSecret", properties);
        Assert.DoesNotContain("ClientId", properties);
    }
}
