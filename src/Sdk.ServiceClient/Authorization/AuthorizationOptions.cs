namespace Meshmakers.Octo.Sdk.ServiceClient.Authorization;

/// <summary>
///     Represents the options for the authorization client.
/// </summary>
public class AuthorizationOptions
{
    /// <summary>
    ///     Issuer URI of the authorization server.
    /// </summary>
    public string IssuerUri { get; set; } = null!;

    /// <summary>
    ///     Client ID of the authorization client.
    /// </summary>
    public string ClientId { get; set; } = null!;

    /// <summary>
    ///     Client secret of the authorization client.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    ///     Tenant ID to include as acr_values in authorization requests.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    ///     Issuer values that are accepted in the discovery document in addition to
    ///     <see cref="IssuerUri" /> (AB#5081). Empty by default, which keeps the strict
    ///     IdentityModel behaviour: the document's <c>issuer</c> must equal the authority the client
    ///     was pointed at.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This exists for <b>split-horizon</b> deployments, where the address a client reaches
    ///         the identity service on is not the address the service knows itself by. The case that
    ///         produced it: an adapter running in a container reaches the host's identity service as
    ///         <c>https://mac.local:5003</c>, while that service issues and advertises
    ///         <c>https://localhost:5003/</c>. Discovery then fails with "Issuer name does not match
    ///         authority" and no token is ever obtained.
    ///     </para>
    ///     <para>
    ///         The inbound direction has had this for a while — <c>Adapter:AdditionalValidIssuers</c>
    ///         decides which issuers a secured route accepts in a presented token. This is its
    ///         outbound counterpart, and it is deliberately shaped the same way: an explicit
    ///         allow-list, never a switch that turns the check off. A blanket
    ///         <c>ValidateIssuerName = false</c> would accept <i>any</i> issuer from whatever host
    ///         answered the discovery request, which is precisely the substitution the check exists
    ///         to prevent.
    ///     </para>
    /// </remarks>
    public string[] AdditionalValidIssuers { get; set; } = [];
}