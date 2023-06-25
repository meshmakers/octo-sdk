using System.Collections.Generic;

namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

public class SocketHubClientOptions : SignalRClientOptions
{
    /// <summary>
    /// Socket object identifier
    /// </summary>
    public string? SocketRtId { get; set; }
    
    public override IDictionary<string, string> Headers => new Dictionary<string, string> { { "socket-rtId", SocketRtId ?? "" } };
}