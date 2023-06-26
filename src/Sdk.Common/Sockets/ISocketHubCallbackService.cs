using Meshmakers.Octo.Communication.Sockets.Contracts.Hubs;

namespace Meshmakers.Octo.Sdk.Common.Sockets;

internal interface ISocketHubCallbackService
{
    void RegisterCallback(ISocketHubCallbacks plugHubCallbacks);
}