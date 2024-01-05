using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Contracts.Hubs;

namespace Meshmakers.Octo.Sdk.Common.Sockets;

/// <summary>
///     Implements the <see cref="ISocketHubCallbackService" /> interface.
/// </summary>
public class SocketHubCallbackService : ISocketHubCallbacks, ISocketHubCallbackService
{
    private ISocketHubCallbacks? _socketHubCallbacks;

    /// <inheritdoc />
    public async Task SocketConfigurationUpdatedAsync(string tenantId, SocketConfigurationDto socketConfiguration)
    {
        var callback = _socketHubCallbacks;
        if (callback != null)
        {
            await callback.SocketConfigurationUpdatedAsync(tenantId, socketConfiguration);
        }
    }

    /// <summary>
    ///     Registers the callback.
    /// </summary>
    /// <param name="plugHubCallbacks"></param>
    public void RegisterCallback(ISocketHubCallbacks plugHubCallbacks)
    {
        _socketHubCallbacks = plugHubCallbacks;
    }
}