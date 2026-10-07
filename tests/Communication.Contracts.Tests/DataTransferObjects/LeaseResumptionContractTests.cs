using System.Text.Json;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Contracts.Hubs;

namespace Communication.Contracts.Tests.DataTransferObjects;

/// <summary>
///     AB#5826 — the contract half of "a lease survives a controller restart".
/// </summary>
/// <remarks>
///     Leases live in the controller's memory. The member therefore has to carry enough on the wire for
///     a controller that forgot the lease to (a) take a running lease over after a reconnect
///     (<see cref="PoolMemberActiveLeaseDto" /> on <c>ResumePoolMemberAsync</c>) and (b) apply a late
///     release to the right execution (<see cref="LeaseResultDto.ExecutionId" />,
///     <see cref="LeaseResultDto.TenantId" />, <see cref="LeaseResultDto.MemberId" />). Every addition is
///     optional and defaulted, so both skew directions keep deserialising.
/// </remarks>
public class LeaseResumptionContractTests
{
    [Fact]
    public void LeaseResult_RoundTripsTheExecutionTheTenantAndTheMember()
    {
        var result = new LeaseResultDto
        {
            LeaseId = "lease-1",
            Reason = LeaseReleaseReasonDto.Completed,
            Success = true,
            ExecutionId = "exec-1",
            TenantId = "borrower",
            MemberId = "pool-member-0"
        };

        var back = JsonSerializer.Deserialize<LeaseResultDto>(JsonSerializer.Serialize(result))!;

        Assert.Equal("exec-1", back.ExecutionId);
        Assert.Equal("borrower", back.TenantId);
        Assert.Equal("pool-member-0", back.MemberId);
    }

    /// <summary>
    ///     A member built before AB#5826 sends none of the three. The controller must see null, not an
    ///     empty string it might look up.
    /// </summary>
    [Fact]
    public void LeaseResult_FromAnOlderMember_DeserialisesWithoutTheNewFields()
    {
        var back = JsonSerializer.Deserialize<LeaseResultDto>(
            """{"LeaseId":"lease-1","Reason":0,"Success":true}""")!;

        Assert.Equal("lease-1", back.LeaseId);
        Assert.Null(back.ExecutionId);
        Assert.Null(back.TenantId);
        Assert.Null(back.MemberId);
    }

    [Fact]
    public void Registration_WithoutAnActiveLease_IsTheIdleShape()
    {
        var back = JsonSerializer.Deserialize<PoolMemberRegistrationDto>(
            """{"AdapterPoolTenantId":"lender","AdapterPoolRtId":"665f0000000000000000ee21","MemberId":"m"}""")!;

        Assert.Null(back.ActiveLease);
    }

    [Fact]
    public void Registration_RoundTripsTheActiveLease()
    {
        var granted = new DateTime(2026, 10, 8, 7, 0, 0, DateTimeKind.Utc);
        var registration = new PoolMemberRegistrationDto
        {
            AdapterPoolTenantId = "lender",
            AdapterPoolRtId = "665f0000000000000000ee21",
            MemberId = "pool-member-0",
            ActiveLease = new PoolMemberActiveLeaseDto
            {
                LeaseId = "lease-1",
                TenantId = "borrower",
                ExecutionId = "exec-1",
                AdapterRtId = "665f0000000000000000d101",
                AdapterCkTypeId = "System.Communication/MeshAdapter",
                GrantedAtUtc = granted,
                ExpiresAtUtc = granted.AddMinutes(15)
            }
        };

        var back = JsonSerializer.Deserialize<PoolMemberRegistrationDto>(JsonSerializer.Serialize(registration))!;

        Assert.Equal(registration.ActiveLease, back.ActiveLease);
    }

    /// <summary>
    ///     🔴 The lease travels back as identifiers only. The borrower's credentials and payload went one
    ///     way on <see cref="LeaseDto" />; a resumption that echoed them would put a second copy into
    ///     every log and store that touches a registration.
    /// </summary>
    [Fact]
    public void ActiveLease_CarriesNoCredentialAndNoPayload()
    {
        var properties = typeof(PoolMemberActiveLeaseDto).GetProperties().Select(p => p.Name).ToList();

        foreach (var forbidden in new[]
                 {
                     nameof(LeaseDto.ClientSecret), nameof(LeaseDto.ClientId), nameof(LeaseDto.DatabasePassword),
                     nameof(LeaseDto.DatabaseUser), nameof(LeaseDto.CallerAccessToken), nameof(LeaseDto.Pipeline),
                     nameof(LeaseDto.PipelineInput)
                 })
        {
            Assert.DoesNotContain(forbidden, properties);
        }
    }

    [Fact]
    public void RegistrationResult_FromAnOlderController_ReadsAsNotAdopted()
    {
        var back = JsonSerializer.Deserialize<PoolMemberRegistrationResultDto>(
            """{"Accepted":true,"MemberId":"m","HeartbeatIntervalSeconds":30}""")!;

        Assert.False(back.ActiveLeaseAdopted);
    }

    /// <summary>
    ///     The default implementation answers the way an older controller does on the wire —
    ///     "not supported" — so a hand-written double that does not implement the method drives the
    ///     member into its fallback instead of into a silent success.
    /// </summary>
    [Fact]
    public async Task ResumePoolMember_DefaultImplementation_IsNotSupported()
    {
        IAdapterPoolHub hub = new MinimalHub();

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            hub.ResumePoolMemberAsync(new PoolMemberRegistrationDto()));
    }

    private sealed class MinimalHub : IAdapterPoolHub
    {
        public Task<PoolMemberRegistrationResultDto> RegisterPoolMemberAsync(PoolMemberRegistrationDto registration) =>
            Task.FromResult(new PoolMemberRegistrationResultDto { Accepted = true });

        public Task ReleaseLeaseAsync(LeaseResultDto result) => Task.CompletedTask;

        public Task HeartbeatAsync(PoolMemberHeartbeatDto heartbeat) => Task.CompletedTask;
    }
}
