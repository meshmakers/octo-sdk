using IdentityModel.Client;
using Meshmakers.Common.Shared;
using Microsoft.Extensions.Options;

// ReSharper disable UnusedType.Global

namespace Meshmakers.Octo.Sdk.ServiceClient.Authorization;

/// <summary>
///     Implements <see cref="IAuthorizationClient" /> using IdentityModel.
/// </summary>
public class AuthorizationClient : IAuthorizationClient
{
    private IDiscoveryCache? _cache;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AuthorizationClient" /> class.
    /// </summary>
    /// <param name="options"></param>
    public AuthorizationClient(IOptionsMonitor<AuthorizationOptions> options)
    {
        Options = options.CurrentValue;

        options.OnChange(CreateCache);
        if (!string.IsNullOrWhiteSpace(options.CurrentValue.IssuerUri))
        {
            CreateCache(options.CurrentValue);
        }
    }

    private IDiscoveryCache Cache
    {
        get
        {
            if (_cache == null)
            {
                throw new ServiceConfigurationMissingException("Discovery cache not initialized.");
            }

            return _cache;
        }
    }

    // ReSharper disable once MemberCanBePrivate.Global
    /// <summary>
    ///     Gets the options used to configure the client.
    /// </summary>
    protected AuthorizationOptions Options { get; private set; }

    /// <inheritdoc />
    public async Task<UserInfoData> GetUserInfoAsync(string accessToken)
    {
        ArgumentValidation.ValidateString(nameof(accessToken), accessToken);

        var disco = await GetDiscoveryResponse();

        var client = new HttpClient();

        var response = await client.GetUserInfoAsync(new UserInfoRequest
        {
            Address = Rebase(disco.UserInfoEndpoint),
            Token = accessToken
        });

        return response.IsError ? new UserInfoData(false, null) : new UserInfoData(true, response.Claims);
    }

    /// <inheritdoc />
    public async Task<bool> IntrospectApiResource(string accessToken, string apiName, string apiSecret)
    {
        ArgumentValidation.ValidateString(nameof(accessToken), accessToken);
        ArgumentValidation.ValidateString(nameof(apiName), apiName);
        ArgumentValidation.ValidateString(nameof(apiSecret), apiSecret);

        var disco = await GetDiscoveryResponse();

        var client = new HttpClient();
        var result = await client.IntrospectTokenAsync(new TokenIntrospectionRequest
        {
            Address = Rebase(disco.IntrospectionEndpoint),

            ClientId = apiName,
            ClientSecret = apiSecret,

            Token = accessToken
        });

        return !result.IsError && result.IsActive;
    }

    private void CreateCache(AuthorizationOptions authorizationOptions)
    {
        Options = authorizationOptions;

        if (string.IsNullOrWhiteSpace(Options.IssuerUri))
        {
            throw new ServiceConfigurationMissingException("Issuer URI is not configured.");
        }

        var url = new Uri(Options.IssuerUri);
        var authority = url.AbsoluteUri.TrimEnd('/');

        if (Options.AdditionalValidIssuers.Length == 0)
        {
            _cache = new DiscoveryCache(authority);
            return;
        }

        // AB#5081: split horizon. IdentityModel's default policy makes two separate checks, and both
        // fail when the address we reach the service on differs from the one it knows itself by:
        //
        //   ValidateIssuerName — the document's `issuer` must equal the authority we asked for.
        //                        Switched off here and replaced by ValidateIssuer() below, which
        //                        compares against an explicit allow-list instead of accepting any.
        //   ValidateEndpoints  — every endpoint URL in the document must sit on the authority's
        //                        host. This one is *kept on* and simply told about the other names
        //                        the service is known by, which is exactly what
        //                        AdditionalEndpointBaseAddresses is for.
        //
        // Turning endpoint validation off instead would let a compromised discovery document point
        // the token request at an arbitrary host — the substitution both checks exist to prevent.
        var policy = new DiscoveryPolicy { ValidateIssuerName = false };
        foreach (var additional in Options.AdditionalValidIssuers)
        {
            if (additional is not null && additional.Length != 0)
            {
                policy.AdditionalEndpointBaseAddresses.Add(additional.TrimEnd('/'));
            }
        }

        _cache = new DiscoveryCache(authority, policy);
    }

    /// <summary>
    ///     Rewrites an endpoint the discovery document advertises under one of
    ///     <see cref="AuthorizationOptions.AdditionalValidIssuers" /> onto the authority this client
    ///     was actually configured with (AB#5081). Returns the value unchanged when no allow-list is
    ///     configured, when the endpoint already sits on the authority, or when it sits somewhere
    ///     else entirely.
    /// </summary>
    /// <remarks>
    ///     🔴 <b>Accepting a foreign endpoint host is not enough — it has to be reachable.</b> In the
    ///     split-horizon case the document names an address the client cannot dial: a container
    ///     reaches the host's identity service as <c>https://mac.local:5003</c>, while the document
    ///     advertises every endpoint under <c>https://localhost:5003</c>, which inside that container
    ///     is the container itself. Merely widening the validation would turn "issuer name does not
    ///     match authority" into "connection refused".
    ///     <para>
    ///         Rebasing is also <b>stricter</b> than IdentityModel's default, not looser: afterwards
    ///         this client only ever talks to the host it was configured to talk to. The default
    ///         follows whatever host the document names, so a substituted document can redirect the
    ///         token request; here it cannot.
    ///     </para>
    ///     <para>
    ///         Every <c>disco.*Endpoint</c> read goes through this method. A new call site that
    ///         forgets it keeps working everywhere except split-horizon, where it fails at connect
    ///         time — so add the wrapper when you add the site.
    ///     </para>
    /// </remarks>
    /// <param name="endpoint">Endpoint URL from the discovery document.</param>
    /// <returns>The endpoint, rebased onto the configured authority where applicable.</returns>
    protected string? Rebase(string? endpoint)
    {
        // Explicit null checks rather than string.IsNullOrWhiteSpace: this project also targets
        // netstandard2.0, where that method carries no [NotNullWhen] annotation, so the compiler
        // does not narrow the reference and the nullable analysis fails the build.
        if (Options.AdditionalValidIssuers.Length == 0 || endpoint is null || endpoint.Length == 0)
        {
            return endpoint;
        }

        var authority = Options.IssuerUri.TrimEnd('/');

        foreach (var alias in Options.AdditionalValidIssuers)
        {
            if (alias is null || alias.Length == 0)
            {
                continue;
            }

            var prefix = alias.TrimEnd('/');
            if (prefix.Length != 0 && endpoint.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return authority + endpoint.Substring(prefix.Length);
            }
        }

        return endpoint;
    }

    /// <summary>
    ///     Accepts the discovery document's issuer when it matches the configured authority or one of
    ///     <see cref="AuthorizationOptions.AdditionalValidIssuers" />. Only reached when that list is
    ///     non-empty; otherwise IdentityModel has already enforced the strict rule.
    /// </summary>
    private void ValidateIssuer(DiscoveryDocumentResponse disco)
    {
        if (Options.AdditionalValidIssuers.Length == 0)
        {
            return;
        }

        var issuer = disco.Issuer?.TrimEnd('/');
        var authority = Options.IssuerUri.TrimEnd('/');

        if (string.Equals(issuer, authority, StringComparison.OrdinalIgnoreCase) ||
            Options.AdditionalValidIssuers.Any(i =>
                string.Equals(issuer, i?.TrimEnd('/'), StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        throw AuthorizationFailedException.AuthenticationFailed(
            $"Issuer name '{disco.Issuer}' matches neither the configured authority '{authority}' nor any " +
            "entry of AdditionalValidIssuers", null);
    }

    private static void ValidateResponse(ProtocolResponse response)
    {
        if (response.IsError)
        {
            throw AuthorizationFailedException.AuthenticationFailed(response.Error, response.Exception);
        }
    }

    // ReSharper disable once MemberCanBePrivate.Global
    /// <summary>
    ///     Gets the discovery response.
    /// </summary>
    /// <returns></returns>
    protected async Task<DiscoveryDocumentResponse> GetDiscoveryResponse()
    {
        var disco = await Cache.GetAsync();
        ValidateResponse(disco);
        ValidateIssuer(disco);

        return disco;
    }
}