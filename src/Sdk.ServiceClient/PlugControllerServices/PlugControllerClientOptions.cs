using System.Collections.Generic;

namespace Meshmakers.Octo.Sdk.ServiceClient.PlugControllerServices;

public class PlugControllerClientOptions : SignalRClientOptions
{
    /// <summary>
    /// Plug object identifier
    /// </summary>
    public string? PlugRtId { get; set; }
    
    public override IDictionary<string, string> Headers => new Dictionary<string, string> { { "plug-rtId", PlugRtId ?? "" } };

}