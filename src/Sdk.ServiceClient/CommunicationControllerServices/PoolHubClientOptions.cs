using System.Collections.Generic;

namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

public class PoolHubClientOptions : SignalRClientOptions
{
    /// <summary>
    /// Plug object identifier
    /// </summary>
    public string? PlugPoolName { get; set; }

    public override IDictionary<string, string> Headers => new Dictionary<string, string> { { "plug-pool-name", PlugPoolName ?? "" } };
}