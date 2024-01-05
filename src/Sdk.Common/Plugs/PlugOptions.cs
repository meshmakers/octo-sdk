namespace Meshmakers.Octo.Sdk.Common.Plugs;

/// <summary>
///     Represents the plug options
/// </summary>
public class PlugOptions
{
    /// <summary>
    ///     Constructor
    /// </summary>
    public PlugOptions()
    {
        TenantId = "meshTest";
        CommunicationControllerServicesUri = "https://localhost:5015";
        UseBroker = true;
        BrokerHost = "localhost";
        BrokerVirtualHost = "/";
        BrokerPort = 5672;
        BrokerUsername = "guest";
        BrokerPassword = "guest";
    }

    /// <summary>
    ///     Gets or sets the plug id
    /// </summary>
    public string? PlugRtId { get; set; }

    /// <summary>
    ///     Gets or sets the tenant id
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    ///     Gets or sets the communication controller services uri
    /// </summary>
    public string? CommunicationControllerServicesUri { get; set; }

    /// <summary>
    ///     Gets or sets if the plug should use the broker
    /// </summary>
    public bool UseBroker { get; set; }

    /// <summary>
    ///     Gets or sets the RabbitMQ broker host name
    /// </summary>
    public string BrokerHost { get; set; }

    /// <summary>
    ///     Gets or sets the RabbitMQ broker virtual host
    /// </summary>
    public string BrokerVirtualHost { get; set; }

    /// <summary>
    ///     Gets or sets the RabbitMQ broker port
    /// </summary>
    public ushort BrokerPort { get; set; }

    /// <summary>
    ///     Gets or sets the RabbitMQ broker username
    /// </summary>
    public string? BrokerUsername { get; set; }

    /// <summary>
    ///     Gets or sets the RabbitMQ broker password
    /// </summary>
    public string? BrokerPassword { get; set; }
}