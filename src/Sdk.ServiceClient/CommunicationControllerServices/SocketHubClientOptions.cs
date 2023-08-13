namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

/// <summary>
/// Options for the <see cref="SocketHubClient"/>.
/// </summary>
public class SocketHubClientOptions : SignalRClientOptions
{
    /// <summary>
    /// Socket object identifier
    /// </summary>
    public string? SocketRtId { get; set; }
    
    /// <summary>
    /// Extra HTTP headers to send with the request.
    /// </summary>
    public override IDictionary<string, string> Headers => new Dictionary<string, string> { { "socket-rtId", SocketRtId ?? "" } };
}