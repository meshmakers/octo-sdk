using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Meshmakers.Octo.Communication.Contracts.Hubs;

/// <summary>
///     Interface of the adapter hub that is responsible for registering and unregistering adapters and managing their state.
/// </summary>
public interface IAdapterHub
{
    /// <summary>
    ///     Registers an adapter at the communication controller
    /// </summary>
    /// <param name="adapterRtEntityId">Object identifier of the adapter</param>
    /// <returns></returns>
    Task<AdapterConfigurationDto> RegisterAdapterAsync(RtEntityId adapterRtEntityId);

    /// <summary>
    ///     Unregisters an adapter from the communication controller
    /// </summary>
    /// <param name="adapterRtEntityId">Object identifier of the adapter</param>
    /// <returns></returns>
    Task UnRegisterAdapterAsync(RtEntityId adapterRtEntityId);
    
    /// <summary>
    ///     Sends debug data to the communication controller
    /// </summary>
    /// <param name="adapterRtEntityId">Object identifier of the adapter</param>
    /// <param name="pipelineRtEntityId">Object identifier of the pipeline</param>
    /// <param name="debugData">Serialized debug data as string</param>
    /// <returns></returns>
    Task SendDebugDataAsync(RtEntityId adapterRtEntityId, RtEntityId pipelineRtEntityId, string debugData);
}