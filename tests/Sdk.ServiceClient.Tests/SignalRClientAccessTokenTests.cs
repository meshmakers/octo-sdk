using System.Reflection;
using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Sdk.ServiceClient.Tests;

/// <summary>
///     AB#5062 — the SignalR transport must present the <see cref="IServiceClientAccessToken" /> it is
///     handed, and must present nothing at all when that token is blank. Before AB#5062 the client wrote
///     a literal <c>Authorization: Bearer your-access-token</c> header and never read the token object.
/// </summary>
public class SignalRClientAccessTokenTests
{
    private readonly ILogger<SignalRClient<SignalRClientOptions>> _logger =
        A.Fake<ILogger<SignalRClient<SignalRClientOptions>>>();

    private static SignalRClientOptions CreateOptions() => new()
    {
        EndpointUri = "https://localhost:5015",
        TenantId = "testTenant"
    };

    private SignalRClient<SignalRClientOptions> CreateClient(IServiceClientAccessToken accessToken,
        SignalRClientOptions? options = null) =>
        new(options ?? CreateOptions(), _logger, accessToken, "testHub");

    [Fact]
    public async Task AccessTokenProvider_ReturnsTheConfiguredToken()
    {
        var accessToken = new ServiceClientAccessToken { AccessToken = "the-real-token" };
        var client = CreateClient(accessToken);
        var connectionOptions = new HttpConnectionOptions();

        client.ConfigureHttpConnectionOptions(connectionOptions);

        Assert.NotNull(connectionOptions.AccessTokenProvider);
        Assert.Equal("the-real-token", await connectionOptions.AccessTokenProvider!());
    }

    [Fact]
    public async Task AccessTokenProvider_IsNotFrozen_ButReReadOnEveryInvocation()
    {
        // The reconnect loop reuses one HubConnection for the process lifetime, so a token captured
        // when the connection object was built would be presented long after it expired.
        var accessToken = new ServiceClientAccessToken { AccessToken = "first-token" };
        var client = CreateClient(accessToken);
        var connectionOptions = new HttpConnectionOptions();

        client.ConfigureHttpConnectionOptions(connectionOptions);
        Assert.Equal("first-token", await connectionOptions.AccessTokenProvider!());

        accessToken.AccessToken = "refreshed-token";

        Assert.Equal("refreshed-token", await connectionOptions.AccessTokenProvider!());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AccessTokenProvider_WithoutAToken_SendsNoCredentialAtAll(string? token)
    {
        // An unconfigured client must look anonymous. A blank-but-present bearer would reach the
        // server as a failed authentication instead of an absent one and pollute the AB#5059
        // LogOnly consumer inventory on /operatorHub.
        var accessToken = new ServiceClientAccessToken { AccessToken = token };
        var client = CreateClient(accessToken);
        var connectionOptions = new HttpConnectionOptions();

        client.ConfigureHttpConnectionOptions(connectionOptions);

        Assert.Null(await connectionOptions.AccessTokenProvider!());
        Assert.DoesNotContain("Authorization", connectionOptions.Headers.Keys);
    }

    [Fact]
    public void ConfigureHttpConnectionOptions_NeverWritesAPlaceholderAuthorizationHeader()
    {
        var accessToken = new ServiceClientAccessToken { AccessToken = "the-real-token" };
        var client = CreateClient(accessToken);
        var connectionOptions = new HttpConnectionOptions();

        client.ConfigureHttpConnectionOptions(connectionOptions);

        // The header path cannot carry the credential: SignalR sends no headers on the WebSocket
        // and SSE transports. The provider is the only mechanism that covers all of them.
        Assert.DoesNotContain("Authorization", connectionOptions.Headers.Keys);
    }

    [Fact]
    public void ConfigureHttpConnectionOptions_StillCopiesTheCallerSuppliedHeaders()
    {
        var options = CreateOptions();
        options.Headers["X-Octo-Test"] = "value";
        var client = CreateClient(new ServiceClientAccessToken(), options);
        var connectionOptions = new HttpConnectionOptions();

        client.ConfigureHttpConnectionOptions(connectionOptions);

        Assert.Equal("value", connectionOptions.Headers["X-Octo-Test"]);
    }

    [Fact]
    public async Task CreatedHubConnection_CarriesTheAccessTokenProvider()
    {
        // Proves the wiring, not just the helper: the connection the client actually builds must be
        // the one carrying the provider.
        var accessToken = new ServiceClientAccessToken { AccessToken = "wired-token" };
        var client = new TestableSignalRClient(CreateOptions(), _logger, accessToken, "testHub");

        var connectionOptions = FindHttpConnectionOptions(client.GetHubConnection());

        Assert.NotNull(connectionOptions);
        Assert.NotNull(connectionOptions!.AccessTokenProvider);
        Assert.Equal("wired-token", await connectionOptions.AccessTokenProvider!());

        await client.StopAsync();
    }

    /// <summary>
    ///     Walks the private object graph of a built <see cref="HubConnection" /> for the
    ///     <see cref="HttpConnectionOptions" /> the connection factory holds. There is no public
    ///     accessor for it; the alternative would be a live hub.
    /// </summary>
    private static HttpConnectionOptions? FindHttpConnectionOptions(object root, int depth = 0)
    {
        if (depth > 4)
        {
            return null;
        }

        foreach (var field in root.GetType()
                     .GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
        {
            object? value;
            try
            {
                value = field.GetValue(root);
            }
            catch (Exception)
            {
                continue;
            }

            switch (value)
            {
                case null:
                    continue;
                case HttpConnectionOptions httpConnectionOptions:
                    return httpConnectionOptions;
            }

            if (value.GetType().Namespace?.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) != true)
            {
                continue;
            }

            var found = FindHttpConnectionOptions(value, depth + 1);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private sealed class TestableSignalRClient : SignalRClient<SignalRClientOptions>
    {
        public TestableSignalRClient(SignalRClientOptions options,
            ILogger<SignalRClient<SignalRClientOptions>> logger,
            IServiceClientAccessToken accessToken, string hubName)
            : base(options, logger, accessToken, hubName)
        {
        }

        public HubConnection GetHubConnection() => HubConnection;
    }
}
