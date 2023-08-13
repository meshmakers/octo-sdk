namespace Meshmakers.Octo.Sdk.ServiceClient;

/// <summary>
/// Represents the options for a SignalR client.
/// </summary>
public class SignalRClientOptions : ServiceClientOptions
{
    /// <summary>
    ///     The tenant id
    /// </summary>
    public string? TenantId { get; set; }
    
    /// <summary>
    /// Optional HTTP headers to send with the request.
    /// </summary>
    public virtual IDictionary<string, string> Headers { get; } = new Dictionary<string, string>();
}