using Meshmakers.Octo.Communication.Contracts.Hubs;

namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

/// <summary>
///     Client proxy interface for the adapter pool management hub (AB#4924).
/// </summary>
public interface IAdapterPoolHubClient : ISignalRClient<AdapterPoolHubClientOptions>, IAdapterPoolHub
{
}
