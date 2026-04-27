using Meshmakers.Octo.Communication.Contracts.Hubs;

namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

/// <summary>
/// Client proxy interface for the operator management hub.
/// </summary>
public interface IOperatorHubClient : ISignalRClient<OperatorHubClientOptions>, IOperatorHub
{
}
