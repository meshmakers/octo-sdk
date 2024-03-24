namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

/// <summary>
///     Options for the <see cref="AdapterHubClient" />.
/// </summary>
public class AdapterHubClientOptions : SignalRClientOptions
{
    /// <summary>
    ///     Adapter runtime id
    /// </summary>
    public string? AdapterRtId { get; set; }
    
    /// <summary>
    ///     Adapter construction kit type id
    /// </summary>
    public string? AdapterCkTypeId { get; set; }

    /// <summary>
    ///     Extra HTTP headers to send with the request.
    /// </summary>
    public override IDictionary<string, string> Headers => new Dictionary<string, string>
    {
        { "adapter-rtId", AdapterRtId ?? "" },
        { "adapter-ckTypeId", AdapterCkTypeId ?? "" }
    };
}