namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.System;

/// <summary>
///     Options for the <see cref="AssetServicesClient" />.
/// </summary>
public class AssetServiceClientOptions : ServiceClientOptions
{
    /// <summary>
    ///     The tenant ID used to scope API requests. Tenant CRUD operations
    ///     manage child tenants of this tenant.
    /// </summary>
    public string? TenantId { get; set; }
}