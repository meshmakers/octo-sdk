using System.Threading.Tasks;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

public interface IPoolHubClient
{
    bool IsAlive { get; }
    PoolHubClientOptions Options { get; }
    Task<PoolConfigurationDto> RegisterPoolOperatorAsync(string plugPoolName);
    Task UnregisterPoolOperatorAsync(string plugPoolName);
    Task StartAsync();
    Task StopAsync();
}
