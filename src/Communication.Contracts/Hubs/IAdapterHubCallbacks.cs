using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Meshmakers.Octo.Communication.Contracts.Hubs;

/// <summary>
///     Interfaces of callbacks that can be called by the adapter hub.
/// </summary>
public interface IAdapterHubCallbacks
{
    /// <summary>
    ///     Informs an adapter that its configuration has been updated.
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="adapterConfiguration">The adapter configuration data transfer object</param>
    Task AdapterConfigurationUpdatedAsync(string tenantId, AdapterConfigurationDto adapterConfiguration);
    
    /// <summary>
    ///     Informs an adapter that the tenant is being updated.
    /// </summary>
    /// <remarks>This disconnects the adapter from services. The adapter needs to retry connection after some time.</remarks>
    /// <param name="tenantId">Tenant identifier</param>
    /// <returns></returns>
    Task PreUpdateTenantAsync(string tenantId);
}