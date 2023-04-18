namespace Meshmakers.Octo.Frontend.Client.Tenants;

/// <summary>
///     Configuration options of tenant client
/// </summary>
public class TenantClientOptions : ServiceClientOptions
{
    /// <summary>
    ///     The tenant id
    /// </summary>
    public string TenantId { get; set; }
}
