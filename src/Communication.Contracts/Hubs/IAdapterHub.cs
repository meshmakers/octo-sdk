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
    /// <param name="adapterRtId">Object identifier of the adapter</param>
    /// <returns></returns>
    Task<AdapterConfigurationDto> RegisterAdapterAsync(OctoObjectId adapterRtId);

    /// <summary>
    ///     Unregisters an adapter from the communication controller
    /// </summary>
    /// <param name="adapterRtId">Object identifier of the adapter</param>
    /// <returns></returns>
    Task UnRegisterAdapterAsync(OctoObjectId adapterRtId);
    
    /// <summary>
    ///     Sends debug data to the communication controller
    /// </summary>
    /// <param name="adapterRtId">Object identifier of the adapter</param>
    /// <param name="pipelineRtId">Object identifier of the pipeline</param>
    /// <param name="debugData">Serialized debug data as string</param>
    /// <returns></returns>
    Task SendDebugDataAsync(OctoObjectId adapterRtId, OctoObjectId pipelineRtId, string debugData);
}