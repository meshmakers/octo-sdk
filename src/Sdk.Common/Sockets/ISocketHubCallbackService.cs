using Meshmakers.Octo.Communication.Sockets.Contracts.Hubs;

namespace Meshmakers.Octo.Sdk.Common.Sockets;

public interface ISocketHubCallbackService
{
    void RegisterCallback(ISocketHubCallbacks plugHubCallbacks);
}