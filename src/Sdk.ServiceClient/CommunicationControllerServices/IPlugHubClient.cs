using System.Threading.Tasks;
using Meshmakers.Octo.Common.Shared;
using Meshmakers.Octo.Communication.Plugs.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Plugs.Contracts.Hubs;

namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

public interface IPlugHubClient : IPlugHub
{
    PlugHubClientOptions Options { get; }

    Task StartAsync();
    Task StopAsync();
}
