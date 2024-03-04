namespace Meshmakers.Octo.Sdk.Common.Adapters;

/// <summary>
///     Represents the shutdown configuration for an adapter
/// </summary>
public record AdapterShutdown
{
    /// <summary>
    ///     Returns the tenant id
    /// </summary>
    public string TenantId { get; init; } = null!;
}