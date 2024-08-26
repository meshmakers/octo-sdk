using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Meshmakers.Octo.Communication.Contracts.Hubs;

/// <summary>
///     Interface of the pool hub that is responsible for registering and unregistering pools and managing their state.
/// </summary>
public interface IPoolHub
{
    /// <summary>
    ///     Registers a pool at the controller
    /// </summary>
    /// <param name="poolName">The name of the pool</param>
    /// <returns></returns>
    Task<PoolConfigurationDto> RegisterPoolOperatorAsync(string poolName);

    /// <summary>
    ///     Unregisters a pool from the controller
    /// </summary>
    /// <param name="poolName">The name of the pool</param>
    /// <returns></returns>
    Task UnregisterPoolOperatorAsync(string poolName);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="poolName">The name of the pool</param>
    /// <param name="adapterRtEntityId">Runtime entity id of the adapter</param>
    /// <param name="deployed">True if the adapter is deployed, false otherwise</param>
    /// <returns></returns>
    Task UpdateAdapterDeploymentStateAsync(string poolName, RtEntityId adapterRtEntityId, bool deployed);
}