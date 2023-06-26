using Meshmakers.Octo.Communication.Sockets.Contracts.Hubs;

namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

/// <summary>
/// Interface of the client proxy for socket hub of communication controller services.
/// </summary>
public interface ISocketHubClient : ISignalRClient<SocketHubClientOptions>, ISocketHub
{
}