namespace Meshmakers.Octo.Sdk.Common.Adapters;

/// <summary>
///     Represents the adapter options
/// </summary>
public class AdapterOptions
{
    /// <summary>
    ///     Constructor
    /// </summary>
    public AdapterOptions()
    {
        TenantId = "meshTest";
        CommunicationControllerServicesUri = "https://localhost:5015";
        UseBroker = true;
        BrokerHost = "localhost";
        BrokerVirtualHost = "/";
        BrokerPort = 5672;
        BrokerUsername = "guest";
        BrokerPassword = "guest";
        AdapterCkTypeId = "System.Communication/EdgeAdapter";
        NlogConfigPath = "nlog.config";
    }

    /// <summary>
    ///     Gets or sets the adapter id
    /// </summary>
    public string? AdapterRtId { get; set; }
    
    /// <summary>
    ///     Gets or sets the adapter ck id
    /// </summary>
    public string? AdapterCkTypeId { get; set; }

    /// <summary>
    ///     Gets or sets the tenant id
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    ///     Gets or sets the communication controller services uri
    /// </summary>
    public string? CommunicationControllerServicesUri { get; set; }
    
    /// <summary>
    ///     Gets or sets a value indicating whether the adapter should ignore certificate validation
    /// </summary>
    public bool IgnoreCertificateValidation { get; set; }

    /// <summary>
    ///     Gets or sets if the adapter should use the broker
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
    
    /// <summary>
    ///    Gets or sets the NLog configuration file
    /// </summary>
    public string NlogConfigPath { get; set; }

    /// <summary>
    /// defines if the adapter should run as hosted service or be started manually (e.g only when a client connects)
    /// </summary>
    public bool UseHostedService { get; set; } = true;
}