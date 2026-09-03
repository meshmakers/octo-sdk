using Meshmakers.Octo.Sdk.ServiceClient.Authorization;
using Microsoft.Extensions.Options;

namespace Sdk.ServiceClient.Tests.Authorization;

/// <summary>
///     AB#5081 — rebasing discovery endpoints onto the authority the client actually reached.
/// </summary>
/// <remarks>
///     The split-horizon case these exist for: a container reaches the host's identity service as
///     <c>https://mac.local:5003</c> while the document advertises every endpoint under
///     <c>https://localhost:5003</c>, which inside that container is the container itself. Accepting
///     the foreign host is not enough — the client would then dial an address it cannot reach.
/// </remarks>
public class EndpointRebaseTests
{
    private const string Authority = "https://mac.local:5003";
    private const string Alias = "https://localhost:5003/";

    private static TestableAuthorizationClient Create(params string[] additionalValidIssuers) =>
        new(new AuthorizationOptions
        {
            IssuerUri = Authority,
            ClientId = "irrelevant",
            AdditionalValidIssuers = additionalValidIssuers
        });

    [Fact]
    public void EndpointOnAnAlias_IsRebasedOntoTheAuthority()
    {
        var client = Create(Alias);

        Assert.Equal($"{Authority}/connect/token",
            client.RebasePublic("https://localhost:5003/connect/token"));
    }

    [Fact]
    public void EndpointAlreadyOnTheAuthority_IsUnchanged()
    {
        var client = Create(Alias);

        Assert.Equal($"{Authority}/connect/token",
            client.RebasePublic($"{Authority}/connect/token"));
    }

    /// <summary>
    ///     The safety property: an endpoint on a host nobody allow-listed is left alone rather than
    ///     silently pulled onto the authority. Rebasing is a translation between known names for the
    ///     same service, not a redirect of arbitrary hosts.
    /// </summary>
    [Fact]
    public void EndpointOnAnUnknownHost_IsLeftAlone()
    {
        var client = Create(Alias);

        Assert.Equal("https://evil.example.com/connect/token",
            client.RebasePublic("https://evil.example.com/connect/token"));
    }

    /// <summary>
    ///     🔴 The discriminating case for the default configuration: with no allow-list the method
    ///     must be inert. Every deployment that is not split-horizon runs through this path, so a
    ///     rebase happening here would change behaviour for everyone.
    /// </summary>
    [Fact]
    public void WithoutAnAllowList_NothingIsRebased()
    {
        var client = Create();

        Assert.Equal("https://localhost:5003/connect/token",
            client.RebasePublic("https://localhost:5003/connect/token"));
    }

    [Fact]
    public void NullAndEmptyEndpointsSurviveUntouched()
    {
        var client = Create(Alias);

        Assert.Null(client.RebasePublic(null));
        Assert.Equal(string.Empty, client.RebasePublic(string.Empty));
    }

    /// <summary>
    ///     Trailing slashes differ between what an operator configures and what a document advertises,
    ///     so neither side may depend on them.
    /// </summary>
    [Fact]
    public void TrailingSlashesOnEitherSideDoNotMatter()
    {
        var client = Create("https://localhost:5003");

        Assert.Equal($"{Authority}/connect/token",
            client.RebasePublic("https://localhost:5003/connect/token"));
    }

    private sealed class TestableAuthorizationClient : AuthorizationClient
    {
        public TestableAuthorizationClient(AuthorizationOptions options)
            : base(new StaticMonitor(options))
        {
        }

        public string? RebasePublic(string? endpoint) => Rebase(endpoint);

        private sealed class StaticMonitor(AuthorizationOptions value) : IOptionsMonitor<AuthorizationOptions>
        {
            public AuthorizationOptions CurrentValue { get; } = value;
            public AuthorizationOptions Get(string? name) => CurrentValue;
            public IDisposable? OnChange(Action<AuthorizationOptions, string?> listener) => null;
        }
    }
}
