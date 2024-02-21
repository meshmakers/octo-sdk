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
}