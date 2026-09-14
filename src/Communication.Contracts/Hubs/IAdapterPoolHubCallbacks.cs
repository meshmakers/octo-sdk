using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Meshmakers.Octo.Communication.Contracts.Hubs;

/// <summary>
///     Controller → pool member callbacks of <c>/adapterPoolHub</c> (AB#4924, concept §4).
/// </summary>
public interface IAdapterPoolHubCallbacks
{
    /// <summary>
    ///     Hands the member one tenant for one work item.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The member enters the lease — tenant scope, borrower credential, tenant repository,
    ///         pipeline definitions — executes, and reports back through
    ///         <c>IAdapterPoolHub.ReleaseLeaseAsync</c>. Between two leases it must retain
    ///         <b>nothing</b> tenant-scoped; that is the invariant the whole feature rests on
    ///         (concept §4), and it is a cross-tenant data incident rather than a bug when it breaks.
    ///     </para>
    ///     <para>
    ///         🔴 <see cref="LeaseDto.ClientSecret" /> travels on this call. It is TTL-scoped, must
    ///         never be persisted by the member and must never reach a log target.
    ///     </para>
    /// </remarks>
    Task LeaseAsync(LeaseDto lease);

    /// <summary>
    ///     Asks the member to stop taking leases and shut down once its current work item ends.
    /// </summary>
    /// <remarks>
    ///     Used when the pool scales in (concept §4a) and when a lease expired server-side: a member
    ///     whose release never arrived is drained and restarted rather than re-used, because its
    ///     post-lease cleanliness is unproven (concept §6).
    /// </remarks>
    Task DrainAsync(string reason);
}
