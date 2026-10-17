using Meshmakers.Octo.Sdk.ServiceClient.Authentication;

namespace Sdk.ServiceClient.Tests.Authentication;

/// <summary>AB#6836: exchange per tenant, cached, silently renewed, never a fallback.</summary>
public class TenantTokenProviderTests
{
    private readonly CountingExchange _exchange = new();
    private readonly ManualTimeProvider _time = new();

    private TenantTokenProvider NewProvider(ITenantTokenStore? store = null) => new(_exchange, store, _time);

    [Fact]
    public async Task SecondCallForTheSameTenant_DoesNotExchangeAgain()
    {
        var provider = NewProvider();
        var root = TestJwt.For("root");

        var first = await provider.GetAccessTokenAsync("child", root, TestContext.Current.CancellationToken);
        var second = await provider.GetAccessTokenAsync("child", root, TestContext.Current.CancellationToken);

        Assert.Equal(first, second);
        Assert.Equal(1, _exchange.Calls);
        Assert.Equal("child", AccessTokenInspector.TryGetTenantId(first));
    }

    [Fact]
    public async Task EachTenantGetsItsOwnToken()
    {
        var provider = NewProvider();
        var root = TestJwt.For("root");

        var a = await provider.GetAccessTokenAsync("child-a", root, TestContext.Current.CancellationToken);
        var b = await provider.GetAccessTokenAsync("child-b", root, TestContext.Current.CancellationToken);

        Assert.Equal(2, _exchange.Calls);
        Assert.Equal("child-a", AccessTokenInspector.TryGetTenantId(a));
        Assert.Equal("child-b", AccessTokenInspector.TryGetTenantId(b));
    }

    [Fact]
    public async Task TokenCloseToExpiry_IsReExchangedFromTheSubjectToken()
    {
        var provider = NewProvider();
        var root = TestJwt.For("root");
        await provider.GetAccessTokenAsync("child", root, TestContext.Current.CancellationToken);

        _time.Advance(_exchange.Lifetime - TenantTokenProvider.RenewalMargin + TimeSpan.FromSeconds(1));
        await provider.GetAccessTokenAsync("child", root, TestContext.Current.CancellationToken);

        Assert.Equal(2, _exchange.Calls);
        Assert.All(_exchange.Subjects, s => Assert.Equal(root, s));
    }

    [Fact]
    public async Task TokenWellBeforeExpiry_IsKept()
    {
        var provider = NewProvider();
        var root = TestJwt.For("root");
        await provider.GetAccessTokenAsync("child", root, TestContext.Current.CancellationToken);

        _time.Advance(_exchange.Lifetime - TenantTokenProvider.RenewalMargin - TimeSpan.FromSeconds(5));
        await provider.GetAccessTokenAsync("child", root, TestContext.Current.CancellationToken);

        Assert.Equal(1, _exchange.Calls);
    }

    [Fact]
    public async Task NewSubjectToken_ReplacesTheCachedOne()
    {
        var provider = NewProvider();
        await provider.GetAccessTokenAsync("child", TestJwt.For("root", "old"), TestContext.Current.CancellationToken);
        await provider.GetAccessTokenAsync("child", TestJwt.For("root", "refreshed"), TestContext.Current.CancellationToken);

        Assert.Equal(2, _exchange.Calls);
    }

    [Fact]
    public async Task CacheIsSharedThroughTheStore()
    {
        var store = new InMemoryTenantTokenStore();
        var root = TestJwt.For("root");
        await NewProvider(store).GetAccessTokenAsync("child", root, TestContext.Current.CancellationToken);
        await NewProvider(store).GetAccessTokenAsync("child", root, TestContext.Current.CancellationToken);

        Assert.Equal(1, _exchange.Calls);
    }

    [Theory]
    [InlineData(TenantTokenDenialCause.NotProvisioned)]
    [InlineData(TenantTokenDenialCause.AccessToTenantDenied)]
    [InlineData(TenantTokenDenialCause.ClientNotAllowed)]
    [InlineData(TenantTokenDenialCause.SubjectTokenRejected)]
    public async Task Denial_IsRaisedWithTheCause_AndNothingIsCached(TenantTokenDenialCause cause)
    {
        var provider = NewProvider();
        var root = TestJwt.For("root");
        _exchange.Deny = cause;

        var ex = await Assert.ThrowsAsync<TenantTokenExchangeDeniedException>(
            () => provider.GetAccessTokenAsync("child", root, TestContext.Current.CancellationToken));

        Assert.Equal(cause, ex.Cause);
        Assert.Equal("child", ex.TenantId);
        Assert.Contains("'child'", ex.Message);
        Assert.DoesNotContain(root, ex.Message);

        _exchange.Deny = null;
        await provider.GetAccessTokenAsync("child", root, TestContext.Current.CancellationToken);
        Assert.Equal(2, _exchange.Calls);
    }

    [Fact]
    public async Task TokenForAnotherTenant_IsRefused()
    {
        var provider = NewProvider();
        _exchange.IssueForTenant = "somewhere-else";

        var ex = await Assert.ThrowsAsync<TenantTokenExchangeDeniedException>(
            () => provider.GetAccessTokenAsync("child", TestJwt.For("root"), TestContext.Current.CancellationToken));

        Assert.Equal(TenantTokenDenialCause.InvalidResponse, ex.Cause);
    }

    [Fact]
    public async Task EmptyAccessToken_IsRefused()
    {
        var provider = NewProvider();
        _exchange.IssueEmpty = true;

        var ex = await Assert.ThrowsAsync<TenantTokenExchangeDeniedException>(
            () => provider.GetAccessTokenAsync("child", TestJwt.For("root"), TestContext.Current.CancellationToken));

        Assert.Equal(TenantTokenDenialCause.InvalidResponse, ex.Cause);
    }

    [Fact]
    public async Task BlankSubjectToken_IsRefusedWithoutCallingIdentity()
    {
        var provider = NewProvider();

        await Assert.ThrowsAsync<TenantTokenExchangeDeniedException>(() => provider.GetAccessTokenAsync("child", " ", TestContext.Current.CancellationToken));

        Assert.Equal(0, _exchange.Calls);
    }

    [Fact]
    public void NotProvisionedMessage_NamesTheTenantAndTheRequiredStep()
    {
        var message = new TenantTokenExchangeDeniedException("acme", TenantTokenDenialCause.NotProvisioned).Message;

        Assert.Contains("'acme'", message);
        Assert.Contains("ProvisionCurrentUser -ttid acme", message);
    }

    private sealed class CountingExchange : ITokenExchangeClient
    {
        public TimeSpan Lifetime { get; } = TimeSpan.FromMinutes(60);
        public int Calls { get; private set; }
        public List<string> Subjects { get; } = [];
        public TenantTokenDenialCause? Deny { get; set; }
        public string? IssueForTenant { get; set; }
        public bool IssueEmpty { get; set; }

        public Task<AuthenticationData> ExchangeTokenForTenantAsync(string subjectToken, string targetTenantId,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            Subjects.Add(subjectToken);
            if (Deny is { } cause)
            {
                throw new TenantTokenExchangeDeniedException(targetTenantId, cause, "denied");
            }

            return Task.FromResult(new AuthenticationData
            {
                AccessToken = IssueEmpty ? null : TestJwt.For(IssueForTenant ?? targetTenantId, $"call{Calls}"),
                ExpiresAt = DateTime.Now + Lifetime
            });
        }
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan by) => _now += by;
    }
}
