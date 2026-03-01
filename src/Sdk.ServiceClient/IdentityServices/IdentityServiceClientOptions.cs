namespace Meshmakers.Octo.Sdk.ServiceClient.IdentityServices;

/// <summary>
///     Options for configuration of the identity services client proxy.
/// </summary>
public class IdentityServiceClientOptions : ServiceClientOptions
{
    /// <summary>
    ///     Optional tenant ID for tenant-scoped API access.
    ///     When set, routes use {tenantId}/v1 instead of system/v1.
    /// </summary>
    public string? TenantId { get; set; }
}