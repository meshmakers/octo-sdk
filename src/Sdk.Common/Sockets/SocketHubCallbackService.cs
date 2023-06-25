using System.Threading.Tasks;
using Meshmakers.Octo.Communication.Sockets.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Sockets.Contracts.Hubs;

namespace Meshmakers.Octo.Sdk.Common.Sockets;

public class SocketHubCallbackService : ISocketHubCallbacks, ISocketHubCallbackService
{
    private ISocketHubCallbacks? _socketHubCallbacks;
    
    public async Task SocketConfigurationUpdatedAsync(string tenantId, SocketConfigurationDto socketConfiguration)
    {
        var callback = _socketHubCallbacks;
        if (callback != null)
        {
            await callback.SocketConfigurationUpdatedAsync(tenantId, socketConfiguration);    
        }
    }

    public void RegisterCallback(ISocketHubCallbacks plugHubCallbacks)
    {
        _socketHubCallbacks = plugHubCallbacks;
    }
}