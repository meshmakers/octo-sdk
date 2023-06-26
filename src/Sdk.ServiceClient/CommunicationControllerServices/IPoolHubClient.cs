using Meshmakers.Octo.Communication.Contracts.Hubs;

namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

/// <summary>
/// Interface of the client proxy for pool hub of communication controller services.
/// </summary>
public interface IPoolHubClient: ISignalRClient<PoolHubClientOptions>, IPoolHub
{
}
