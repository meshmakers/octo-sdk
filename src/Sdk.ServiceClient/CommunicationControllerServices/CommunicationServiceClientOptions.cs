namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

/// <summary>
///     Options for the <see cref="CommunicationServiceClientOptions" />.
/// </summary>
public class CommunicationServiceClientOptions : ServiceClientOptions
{
    /// <summary>
    ///     The tenant ID used to scope API requests.
    /// </summary>
    public string? TenantId { get; set; }
}