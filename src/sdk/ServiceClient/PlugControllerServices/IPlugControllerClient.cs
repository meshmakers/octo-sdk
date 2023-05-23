using System.Threading.Tasks;
using Meshmakers.Octo.Common.Shared;
using Meshmakers.Octo.Communication.Plugs.Contracts.DataTransferObjects;

namespace Meshmakers.Octo.Sdk.Client.PlugControllerServices;

public interface IPlugControllerClient
{
    PlugControllerClientOptions Options { get; }
    
    Task<PlugConfigurationDto> RegisterPlugAsync(OctoObjectId plugObjectId);
    Task StartAsync();
    Task StopAsync();
}