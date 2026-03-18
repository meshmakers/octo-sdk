namespace Meshmakers.Octo.Sdk.ServiceClient.IdentityServices;

/// <summary>
///     Options for configuration of the identity services client proxy.
/// </summary>
public class IdentityServiceClientOptions : ServiceClientOptions
{
    /// <summary>
    ///     The tenant ID used to scope API requests. Required for all operations.
    ///     Routes use {tenantId}/v1. Throws if not set.
    /// </summary>
    public string? TenantId { get; set; }
}