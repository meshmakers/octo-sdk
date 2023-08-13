namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

/// <summary>
/// Options for the <see cref="PlugHubClient"/>.
/// </summary>
public class PlugHubClientOptions : SignalRClientOptions
{
    /// <summary>
    /// Plug object identifier.
    /// </summary>
    public string? PlugRtId { get; set; }
    
    /// <summary>
    /// Extra HTTP headers to send with the request.
    /// </summary>
    public override IDictionary<string, string> Headers => new Dictionary<string, string> { { "plug-rtId", PlugRtId ?? "" } };

}