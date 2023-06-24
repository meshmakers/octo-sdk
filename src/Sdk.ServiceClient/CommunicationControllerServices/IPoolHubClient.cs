using System.Threading.Tasks;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

public interface IPoolHubClient
{
    bool IsAlive { get; }
    PoolHubClientOptions Options { get; }
    Task<PoolConfigurationDto> RegisterPlugPoolOperatorAsync(string plugPoolName);
    Task UnregisterPlugPoolOperatorAsync(string plugPoolName);
    Task StartAsync();
    Task StopAsync();
}
