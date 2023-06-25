using System.Threading.Tasks;
using Meshmakers.Octo.Communication.Sockets.Contracts.Hubs;

namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

public interface ISocketHubClient : ISocketHub
{
    SocketHubClientOptions Options { get; }
    Task StartAsync();
    Task StopAsync();
}