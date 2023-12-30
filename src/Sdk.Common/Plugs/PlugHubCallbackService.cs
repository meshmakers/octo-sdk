using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Contracts.Hubs;

namespace Meshmakers.Octo.Sdk.Common.Plugs;

internal class PlugHubCallbackService : IPlugHubCallbacks, IPlugHubCallbackService
{
    private IPlugHubCallbacks? _plugHubCallbacks;
    public async Task PlugConfigurationUpdatedAsync(string tenantId, PlugConfigurationDto plugConfiguration)
    {
        var callback = _plugHubCallbacks;
        if (callback != null)
        {
            await callback.PlugConfigurationUpdatedAsync(tenantId, plugConfiguration);    
        }
    }

    public void RegisterCallback(IPlugHubCallbacks plugHubCallbacks)
    {
        _plugHubCallbacks = plugHubCallbacks;
    }
}