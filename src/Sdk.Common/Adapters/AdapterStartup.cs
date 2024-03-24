using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;

namespace Meshmakers.Octo.Sdk.Common.Adapters;

/// <summary>
///     Represents the startup configuration for an adapter
/// </summary>
public record AdapterStartup
{
    /// <summary>
    ///     Returns the tenant id
    /// </summary>
    public string TenantId { get; init; } = null!;

    /// <summary>
    ///     Returns the received adapter configuration from the backend
    /// </summary>
    public AdapterConfigurationDto Configuration { get; init; } = null!;
    
    /// <summary>
    ///     Sends debug information to the backend
    /// </summary>
    public Func<RtEntityId, string, Task> SendDebugInfoFunc { get; init; } = null!;
}