using Meshmakers.Octo.Communication.Plugs.Contracts.Hubs;

namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

/// <summary>
/// Interface of the client proxy for plug hub of communication controller services.
/// </summary>
public interface IPlugHubClient : ISignalRClient<PlugHubClientOptions>, IPlugHub
{
}
