namespace Meshmakers.Octo.Sdk.ServiceClient.AiServices;

/// <summary>
///     Options for the <see cref="AiServicesClient" />. Every routed call is tenant-scoped, so
///     <see cref="TenantId" /> is <b>required</b>: <c>BuildServiceUri</c> throws
///     <c>ServiceConfigurationMissingException</c> without it (stage 3 of AB#5060, matching what
///     AB#4287 did to the Communication, Reporting and StreamData clients).
/// </summary>
public class AiServiceClientOptions : ServiceClientOptions
{
    /// <summary>
    ///     The tenant ID used to scope API requests; routes to <c>{tenantId}/v1</c>. Required for
    ///     every call that goes through the shared client — the one exception is
    ///     <c>RedeemTicketAsync</c>, which is anonymous, builds its own client against the service
    ///     root and therefore still works without a tenant.
    /// </summary>
    public string? TenantId { get; set; }
}
