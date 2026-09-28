using System.Net.Security;
using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.Extensions.Logging;

namespace Sdk.ServiceClient.Tests;

/// <summary>
///     AB#5303 items 1 and 2 — the one switch, and the fact that it is off unless somebody asks.
/// </summary>
/// <remarks>
///     <para>
///         🔴 The gate is process-wide state, so every test here resets it in a finally. A test that
///         left it open would not fail itself — it would silently turn certificate validation off for
///         every test that ran afterwards, which is a smaller version of the defect being fixed.
///     </para>
///     <para>
///         🔴 <b>Not run in parallel with anything.</b> xUnit runs different collections concurrently,
///         and a shared process-wide flag cannot survive that: one test opening the gate while another
///         asserts it is closed is a race, not a failure. The collection attribute pins them to one.
///     </para>
/// </remarks>
[Collection(nameof(ServerCertificateTrustTests))]
[CollectionDefinition(nameof(ServerCertificateTrustTests), DisableParallelization = true)]
public class ServerCertificateTrustTests : IDisposable
{
    private readonly string? _aspNetEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
    private readonly string? _dotNetEnvironment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", _aspNetEnvironment);
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", _dotNetEnvironment);
        ServerCertificateTrust.ResetForTests();
        GC.SuppressFinalize(this);
    }

    private static void SetEnvironment(string? name)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", name);
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", null);
    }

    /// <summary>The default, and the whole point of item 1: nobody asked, so nothing is bypassed.</summary>
    [Fact]
    public void ByDefaultTheProcessValidates()
    {
        Assert.False(ServerCertificateTrust.AllowsAnyServerCertificate);
        Assert.Null(ServerCertificateTrust.ValidationCallback);

        var handler = Assert.IsType<SocketsHttpHandler>(ServerCertificateTrust.CreateHandler());
        Assert.Null(handler.SslOptions.RemoteCertificateValidationCallback);
    }

    /// <summary>An unset environment is allowed — most adapter pods set neither variable.</summary>
    [Fact]
    public void AnUnsetEnvironmentMayOptOut()
    {
        SetEnvironment(null);

        Assert.True(ServerCertificateTrust.AllowAnyServerCertificate());
        Assert.True(ServerCertificateTrust.AllowsAnyServerCertificate);
        Assert.NotNull(ServerCertificateTrust.ValidationCallback);
    }

    [Fact]
    public void DevelopmentMayOptOut()
    {
        SetEnvironment("Development");

        Assert.True(ServerCertificateTrust.AllowAnyServerCertificate());
    }

    /// <summary>
    ///     🔴 Production refuses, and keeps validating rather than failing the process: a service that
    ///     came up with a bad flag must keep working securely, not stop.
    /// </summary>
    [Theory]
    [InlineData("Production")]
    [InlineData("production")]
    public void ProductionRefusesAndKeepsValidating(string environmentName)
    {
        SetEnvironment(environmentName);

        Assert.False(ServerCertificateTrust.AllowAnyServerCertificate());
        Assert.False(ServerCertificateTrust.AllowsAnyServerCertificate);
        Assert.Null(ServerCertificateTrust.ValidationCallback);
    }

    /// <summary>DOTNET_ENVIRONMENT counts too; a worker host has no ASPNETCORE_ENVIRONMENT.</summary>
    [Fact]
    public void TheDotNetEnvironmentVariableIsHonouredWhenTheAspNetOneIsUnset()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Production");

        Assert.False(ServerCertificateTrust.AllowAnyServerCertificate());
    }

    /// <summary>The refusal is reported; a switch that silently does nothing is what item 2 is about.</summary>
    [Fact]
    public void TheRefusalIsLogged()
    {
        SetEnvironment("Production");
        var logger = A.Fake<ILogger>();
        A.CallTo(() => logger.IsEnabled(A<LogLevel>._)).Returns(true);

        ServerCertificateTrust.AllowAnyServerCertificate(logger);

        A.CallTo(logger)
            .Where(call => call.Method.Name == nameof(ILogger.Log)
                           && call.GetArgument<LogLevel>(0) == LogLevel.Error)
            .MustHaveHappenedOnceExactly();
    }

    /// <summary>Opting out reaches a handler somebody else built — SignalR's shape.</summary>
    [Fact]
    public void ApplyOpensASocketsHandlerOnlyWhileTheGateIsOpen()
    {
        SetEnvironment("Development");

        var closed = (SocketsHttpHandler)ServerCertificateTrust.Apply(new SocketsHttpHandler());
        Assert.Null(closed.SslOptions.RemoteCertificateValidationCallback);

        ServerCertificateTrust.AllowAnyServerCertificate();

        var open = (SocketsHttpHandler)ServerCertificateTrust.Apply(new SocketsHttpHandler());
        Assert.NotNull(open.SslOptions.RemoteCertificateValidationCallback);
    }

    /// <summary>The legacy shape SignalR may still hand us.</summary>
    [Fact]
    public void ApplyAlsoOpensTheLegacyHttpClientHandler()
    {
        SetEnvironment("Development");
        ServerCertificateTrust.AllowAnyServerCertificate();

        var handler = (HttpClientHandler)ServerCertificateTrust.Apply(new HttpClientHandler());

        Assert.NotNull(handler.ServerCertificateCustomValidationCallback);
    }

    /// <summary>
    ///     🔴 The regression that item 1 is: before this, the hub transport attached `=> true`
    ///     unconditionally. The factory must leave the handler alone while the gate is closed.
    /// </summary>
    [Fact]
    public void TheHubTransportValidatesUnlessTheGateIsOpen()
    {
        var client = new SignalRClient<SignalRClientOptions>(
            new SignalRClientOptions { EndpointUri = "https://localhost:5015" },
            A.Fake<ILogger<SignalRClient<SignalRClientOptions>>>(),
            new ServiceClientAccessToken(),
            "testHub");

        var connectionOptions = new HttpConnectionOptions();
        client.ConfigureHttpConnectionOptions(connectionOptions);

        Assert.NotNull(connectionOptions.HttpMessageHandlerFactory);

        var validating = (SocketsHttpHandler)connectionOptions.HttpMessageHandlerFactory!(new SocketsHttpHandler());
        Assert.Null(validating.SslOptions.RemoteCertificateValidationCallback);

        SetEnvironment("Development");
        ServerCertificateTrust.AllowAnyServerCertificate();

        var bypassing = (SocketsHttpHandler)connectionOptions.HttpMessageHandlerFactory!(new SocketsHttpHandler());
        Assert.NotNull(bypassing.SslOptions.RemoteCertificateValidationCallback);
    }
}
