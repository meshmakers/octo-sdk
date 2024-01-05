using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

namespace Meshmakers.Octo.Sdk.Common.Plugs;

/// <summary>
///     Represents the startup configuration for a plug
/// </summary>
public record PlugStartup
{
    /// <summary>
    ///     Returns the tenant id
    /// </summary>
    public string TenantId { get; init; } = null!;

    /// <summary>
    ///     Returns the received plug configuration from the backend
    /// </summary>
    public PlugConfigurationDto Configuration { get; init; } = null!;
}