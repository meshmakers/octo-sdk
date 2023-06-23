using System.Threading.Tasks;
using Meshmakers.Octo.Communication.Plugs.Contracts.DataTransferObjects;

namespace Meshmakers.Octo.Sdk.ServiceClient.PlugControllerServices;

public interface IPlugPoolControllerClient
{
    bool IsAlive { get; }
    PoolControllerClientOptions Options { get; }
    Task<PlugPoolConfigurationDto> RegisterPlugPoolOperatorAsync(string plugPoolName);
    Task UnregisterPlugPoolOperatorAsync(string plugPoolName);
    Task StartAsync();
    Task StopAsync();
}
