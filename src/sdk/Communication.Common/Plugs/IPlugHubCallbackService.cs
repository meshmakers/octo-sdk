using Meshmakers.Octo.Communication.Plugs.Contracts.Hubs;

namespace Meshmakers.Octo.Sdk.PlugExecutor;

public interface IPlugHubCallbackService
{
    void RegisterCallback(IPlugHubCallbacks plugHubCallbacks);
}