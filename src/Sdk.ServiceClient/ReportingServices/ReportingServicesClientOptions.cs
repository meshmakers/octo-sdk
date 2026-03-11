namespace Meshmakers.Octo.Sdk.ServiceClient.ReportingServices;

/// <summary>
///     Options for the <see cref="ReportingServicesClientOptions" />.
/// </summary>
public class ReportingServicesClientOptions : ServiceClientOptions
{
    /// <summary>
    ///     The tenant ID used to scope API requests.
    /// </summary>
    public string? TenantId { get; set; }
}