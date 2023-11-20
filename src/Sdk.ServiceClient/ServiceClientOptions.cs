namespace Meshmakers.Octo.Sdk.ServiceClient;

/// <summary>
///     Configuration options of data source client
/// </summary>
public class ServiceClientOptions
{
    /// <summary>
    ///     The root uri of core services
    /// </summary>
    public string? EndpointUri { get; set; }
    
    /// <summary>
    /// Maximum request duration in milliseconds.
    /// </summary>
    public int MaxTimeout { get; set; } = 100000;
}
