using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

namespace Meshmakers.Octo.Sdk.Common.Sockets;

/// <summary>
///     Represents the startup configuration for a socket
/// </summary>
public record SocketStartup
{
    /// <summary>
    ///     Returns the tenant id
    /// </summary>
    public string TenantId { get; init; } = null!;

    /// <summary>
    ///     Returns the received socket configuration from the backend
    /// </summary>
    public AdapterConfigurationDto Configuration { get; init; } = null!;
}