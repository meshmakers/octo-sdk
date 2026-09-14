namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

/// <summary>
///     Options for the <see cref="AdapterPoolHubClient" /> (AB#4924).
/// </summary>
/// <remarks>
///     🔴 <b>There is deliberately no <c>TenantId</c> in play here.</b> <c>SignalRClientOptions</c>
///     carries one because the base <c>BuildServiceUri</c> builds <c>{endpoint}/{tenant}/{hub}</c>,
///     and <see cref="AdapterPoolHubClient" /> overrides that to reach the tenant-free
///     <c>/adapterPoolHub</c>. A pool member has no tenant of its own — it is handed one per lease —
///     so anything it took from an options tenant would be the wrong answer by construction.
/// </remarks>
public class AdapterPoolHubClientOptions : SignalRClientOptions
{
    /// <summary>
    ///     Stable identity of this member process across reconnects, sent at registration and
    ///     recorded on every execution the member serves as <c>LeasedOnMemberId</c>. A pod name is
    ///     the natural value; when blank the controller substitutes the connection id.
    /// </summary>
    public string? MemberId { get; set; }

    /// <summary>
    ///     The tenant that owns the adapter pool this member belongs to — the <b>lender</b>. It is
    ///     what the connection is authorized against (concept §8, Q4) and never the tenant work runs
    ///     for.
    /// </summary>
    public string? PoolTenantId { get; set; }

    /// <summary>
    ///     RtId of the <c>AdapterPool</c> entity in <see cref="PoolTenantId" />.
    /// </summary>
    public string? PoolRtId { get; set; }
}
