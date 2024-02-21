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
    /// <returns></returns>
    Task AdapterConfigurationUpdatedAsync(string tenantId, AdapterConfigurationDto adapterConfiguration);
}