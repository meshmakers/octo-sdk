namespace Meshmakers.Octo.Sdk.Common.Sockets;

/// <summary>
///     Represents the socket options
/// </summary>
public class SocketOptions
{
    /// <summary>
    ///     Constructor
    /// </summary>
    public SocketOptions()
    {
        TenantId = "meshTest";
        CommunicationControllerServicesUri = "https://localhost:5015";
    }

    /// <summary>
    ///     Gets or sets the adapter id
    /// </summary>
    public string? AdapterRtId { get; set; }

    /// <summary>
    ///     Gets or sets the tenant id
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    ///     Gets or sets the communication controller services uri
    /// </summary>
    public string? CommunicationControllerServicesUri { get; set; }
}