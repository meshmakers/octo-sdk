using System.Reflection;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Contracts.Hubs;
using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;
using Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Sdk.ServiceClient.Tests.CommunicationControllerServices;

/// <summary>
///     AB#4924 increment 6 — the pool member's management connection.
/// </summary>
/// <remarks>
///     🔴 The property under test is that this connection is <b>tenant-free</b>. The base
///     <c>SignalRClient.BuildServiceUri</c> builds <c>{endpoint}/{tenant}/{hub}</c> and throws when
///     the tenant is blank, which is correct for every tenant-addressed hub and is exactly why a pool
///     member — a process that belongs to no tenant — cannot use <c>/{tenantId}/adapterHub</c>
///     (implementation plan §12.6). A regression that let the base implementation run here would not
///     produce a wrong URL; it would produce a startup exception on every pool member, or worse, a
///     member connecting on some tenant's route.
/// </remarks>
public class AdapterPoolHubClientTests
{
    private sealed class ProbeClient(AdapterPoolHubClientOptions options, IAdapterPoolHubCallbacks callbacks)
        : AdapterPoolHubClient(options, A.Fake<ILogger<AdapterPoolHubClient>>(),
            new ServiceClientAccessToken(), callbacks)
    {
        public Uri Probe() => BuildServiceUri();

        public void BindCallbacks(HubConnection hubConnection) => RegisterServerCallbacks(hubConnection);
    }

    private static ProbeClient CreateClient(string? endpointUri, string? tenantId = null,
        IAdapterPoolHubCallbacks? callbacks = null)
    {
        return new ProbeClient(new AdapterPoolHubClientOptions
        {
            EndpointUri = endpointUri,
            TenantId = tenantId,
            MemberId = "pool-member-0",
            PoolTenantId = "lender",
            PoolRtId = "665f0000000000000000ee21"
        }, callbacks ?? A.Fake<IAdapterPoolHubCallbacks>());
    }

    [Fact]
    public void ServiceUri_IsTenantFree()
    {
        Assert.Equal("https://comm.example.com/adapterPoolHub",
            CreateClient("https://comm.example.com").Probe().ToString());
    }

    [Fact]
    public void ServiceUri_WithTrailingSlash_IsTenantFree()
    {
        Assert.Equal("https://comm.example.com/adapterPoolHub",
            CreateClient("https://comm.example.com/").Probe().ToString());
    }

    /// <summary>
    ///     🔴 A tenant on the options must not leak into the route. A pool member that happened to
    ///     carry one — an operator copying an adapter's configuration, say — would otherwise open its
    ///     management channel on a tenant-addressed path and be judged by the wrong gate.
    /// </summary>
    [Fact]
    public void ServiceUri_IgnoresAnyTenantOnTheOptions()
    {
        Assert.Equal("https://comm.example.com/adapterPoolHub",
            CreateClient("https://comm.example.com", "somebody").Probe().ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ServiceUri_WithoutAnEndpoint_Throws(string? endpointUri)
    {
        Assert.Throws<ServiceConfigurationMissingException>(() => CreateClient(endpointUri).Probe());
    }

    /// <summary>
    ///     The route the controller mounts and the route this client dials are two literals that have
    ///     to agree, and nothing at build or deploy time notices a drift — the member would simply
    ///     fail to negotiate. The constant is the single source on this side.
    /// </summary>
    [Fact]
    public void HubName_MatchesTheMountedRoute()
    {
        Assert.Equal("adapterPoolHub", AdapterPoolHubClient.HubName);
    }

    /// <summary>
    ///     Both pushes must be bound on <b>every</b> connection the base class builds. A lost
    ///     <c>LeaseAsync</c> handler is a lease that the controller granted and that nobody acts on:
    ///     the member looks healthy, the work item sits until the TTL expires.
    /// </summary>
    [Fact]
    public void RegisterServerCallbacks_BindsLeaseAndDrain()
    {
        var callbacks = A.Fake<IAdapterPoolHubCallbacks>();
        var client = CreateClient("https://comm.example.com", callbacks: callbacks);
        var hubConnection = new HubConnectionBuilder().WithUrl("https://comm.example.com/adapterPoolHub").Build();

        client.BindCallbacks(hubConnection);

        var bound = BoundMethodNames(hubConnection);
        Assert.Contains(nameof(IAdapterPoolHubCallbacks.LeaseAsync), bound);
        Assert.Contains(nameof(IAdapterPoolHubCallbacks.DrainAsync), bound);
    }

    /// <summary>
    ///     <c>HubConnection</c> exposes no way to enumerate its bound handlers, so the assertion above
    ///     reads the field the client library keeps them in. If a future SignalR release renames it
    ///     this returns nothing and the test fails loudly rather than passing vacuously — which is the
    ///     behaviour to want from a reflection probe.
    /// </summary>
    private static IReadOnlyCollection<string> BoundMethodNames(HubConnection hubConnection)
    {
        var handlersField = typeof(HubConnection).GetField("_handlers",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(handlersField);

        var handlers = handlersField!.GetValue(hubConnection);
        Assert.NotNull(handlers);

        return ((System.Collections.IDictionary)handlers!).Keys.Cast<string>().ToList();
    }
}
