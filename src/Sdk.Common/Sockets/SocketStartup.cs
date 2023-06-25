using Meshmakers.Octo.Communication.Sockets.Contracts.DataTransferObjects;

namespace Meshmakers.Octo.Sdk.Common.Sockets;

public record SocketStartup
{
    public string TenantId { get; init; } = null!;

    public SocketConfigurationDto Configuration { get; init; } = null!;
}