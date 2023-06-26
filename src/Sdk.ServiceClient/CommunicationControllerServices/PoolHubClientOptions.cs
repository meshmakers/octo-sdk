using System.Collections.Generic;

namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

/// <summary>
/// Options for the <see cref="PoolHubClient"/>.
/// </summary>
public class PoolHubClientOptions : SignalRClientOptions
{
    /// <summary>
    /// Name of the pool
    /// </summary>
    public string? PoolName { get; set; }

    /// <summary>
    /// Extra HTTP headers to send with the request.
    /// </summary>
    public override IDictionary<string, string> Headers => new Dictionary<string, string> { { "pool-name", PoolName ?? "" } };
}