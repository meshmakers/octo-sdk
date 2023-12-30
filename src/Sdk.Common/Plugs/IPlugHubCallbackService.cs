using Meshmakers.Octo.Communication.Contracts.Hubs;

namespace Meshmakers.Octo.Sdk.Common.Plugs;

internal interface IPlugHubCallbackService
{
    void RegisterCallback(IPlugHubCallbacks plugHubCallbacks);
}