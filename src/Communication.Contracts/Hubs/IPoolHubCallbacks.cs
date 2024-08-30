using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Meshmakers.Octo.Communication.Contracts.Hubs;

/// <summary>
///     Interfaces of callbacks that can be called by the pool hub.
/// </summary>
public interface IPoolHubCallbacks
{
    /// <summary>
    /// Updates the pool configuration and deploys the communication adapters.
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="poolName">The name of the pool</param>
    /// <param name="poolConfigurationDto">Updated pool configuration</param>
    /// <returns></returns>
    Task UpdatePoolConfigurationAsync(string tenantId, string poolName, PoolConfigurationDto poolConfigurationDto);
    
    /// <summary>
    ///     Informs the pool that a new communication adapter has to be deployed.
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="poolCommunicationAdapter">Communication adapter data transfer object</param>
    /// <returns></returns>
    Task DeployCommunicationAdapterAsync(string tenantId, PoolCommunicationAdapterDto poolCommunicationAdapter);

    /// <summary>
    ///     Inform the pool that a communication adapter has to be removed from deployment.
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="poolCommunicationAdapter">Communication adapter data transfer object</param>
    /// <returns></returns>
    Task UndeployCommunicationAdapterAsync(string tenantId, PoolCommunicationAdapterDto poolCommunicationAdapter);
    
    /// <summary>
    ///     Informs a pool that the tenant is being updated.
    /// </summary>
    /// <remarks>This disconnects the pool from services. The pool needs to retry connection after some time.</remarks>
    /// <param name="tenantId">Tenant identifier</param>
    /// <returns></returns>
    Task PreUpdateTenantAsync(string tenantId);
}