namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.System;

/// <summary>
///     Options for the <see cref="AssetServicesClient" />.
/// </summary>
public class AssetServiceClientOptions : ServiceClientOptions
{
    /// <summary>
    ///     The tenant ID used to scope API requests. Required for all operations.
    ///     Tenant CRUD operations manage child tenants of this tenant.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    ///     Timeout of file byte transfers of <see cref="AssetServicesClient.Files" /> (AB#6229): the whole upload,
    ///     a download until its response headers arrive. Default one hour, so 100 MB over a slow uplink works;
    ///     zero or negative = no timeout. Other calls keep <see cref="ServiceClientOptions.MaxTimeout" />.
    /// </summary>
    public TimeSpan FileTransferTimeout { get; set; } = TimeSpan.FromHours(1);
}