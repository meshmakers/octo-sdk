using System.Text.Json;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

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
        GrantedAtUtc = new DateTime(2026, 9, 14, 10, 0, 0, DateTimeKind.Utc),
        ExpiresAtUtc = new DateTime(2026, 9, 14, 10, 5, 0, DateTimeKind.Utc)
    };

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
}
