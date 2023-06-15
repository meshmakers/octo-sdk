namespace Meshmakers.Octo.Sdk.Common.Plugs;

/// <summary>
/// Represents the plug options
/// </summary>
public class PlugOptions
{
    /// <summary>
    /// Constructor
    /// </summary>
    public PlugOptions()
    {
        TenantId = "meshTest";
        PlugControllerServicesUri = "https://localhost:5015";
        BrokerHost = "localhost";
        BrokerVirtualHost = "/";
        BrokerPort = 5672;
        BrokerUsername = "guest";
        BrokerPassword = "guest";
    }
    
    /// <summary>
    /// Gets or sets the plug id
    /// </summary>
    public string? PlugId { get; set; }
    
    /// <summary>
    /// Gets or sets the tenant id
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the plug controller services uri
    /// </summary>
    public string? PlugControllerServicesUri { get; set; }
    
    /// <summary>
    /// Gets or sets the RabbitMQ broker host name
    /// </summary>
    public string BrokerHost { get; set; }
    
    /// <summary>
    /// Gets or sets the RabbitMQ broker virtual host
    /// </summary>
    public string BrokerVirtualHost { get; set; }
    
    /// <summary>
    /// Gets or sets the RabbitMQ broker port
    /// </summary>
    public ushort BrokerPort { get; set; }
    
    /// <summary>
    /// Gets or sets the RabbitMQ broker username
    /// </summary>
    public string? BrokerUsername { get; set; }
    
    /// <summary>
    /// Gets or sets the RabbitMQ broker password
    /// </summary>
    public string? BrokerPassword { get; set; }
}