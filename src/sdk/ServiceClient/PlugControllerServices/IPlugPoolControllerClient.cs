using System.Threading.Tasks;
using Meshmakers.Octo.Communication.Plugs.Contracts.DataTransferObjects;

namespace Meshmakers.Octo.Sdk.Client.PlugControllerServices;

public interface IPlugPoolControllerClient
{
    PlugControllerClientOptions Options { get; }
    Task<PlugPoolConfigurationDto> RegisterPlugPoolAsync(string plugPoolName);
    Task StartAsync();
    Task StopAsync();
}