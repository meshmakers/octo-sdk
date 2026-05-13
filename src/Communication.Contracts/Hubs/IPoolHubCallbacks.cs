// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Meshmakers.Octo.Communication.Contracts.Hubs;

/// <summary>
///     Interfaces of callbacks that can be called by the pool hub.
/// </summary>
public interface IPoolHubCallbacks
{
    /// <summary>
    ///     Informs a pool that the tenant is being updated.
    /// </summary>
    /// <remarks>This disconnects the pool from services. The pool needs to retry connection after some time.</remarks>
    /// <param name="tenantId">Tenant identifier</param>
    /// <returns></returns>
    Task PreUpdateTenantAsync(string tenantId);
}
