using Meshmakers.Octo.Communication.Sockets.Contracts.Hubs;

namespace Meshmakers.Octo.Sdk.Common.Sockets;

/// <summary>
/// Registers a callback.
/// </summary>
public interface ISocketHubCallbackService
{
    /// <summary>
    /// Registers a callback.
    /// </summary>
    /// <param name="plugHubCallbacks"></param>
    void RegisterCallback(ISocketHubCallbacks plugHubCallbacks);
}