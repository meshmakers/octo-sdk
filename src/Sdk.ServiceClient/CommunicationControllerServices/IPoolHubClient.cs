using System.Threading.Tasks;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

public interface IPoolHubClient
{
    bool IsAlive { get; }
    PoolHubClientOptions Options { get; }
    Task<PoolConfigurationDto> RegisterPoolOperatorAsync(string poolName);
    Task UnregisterPoolOperatorAsync(string poolName);
    Task StartAsync();
    Task StopAsync();
}
