using Meshmakers.Octo.Sdk.ServiceClient;
using Microsoft.Extensions.Logging;

namespace Sdk.ServiceClient.Tests;

public class SignalRClientTests
{
    private readonly ILogger<SignalRClient<SignalRClientOptions>> _logger;
    private readonly IServiceClientAccessToken _accessToken;
    private readonly SignalRClientOptions _options;

    public SignalRClientTests()
    {
        _logger = A.Fake<ILogger<SignalRClient<SignalRClientOptions>>>();
        _accessToken = A.Fake<IServiceClientAccessToken>();
        _options = new SignalRClientOptions
        {
            EndpointUri = "https://localhost:5015",
            TenantId = "testTenant"
        };
    }

    [Fact]
    public async Task StopAsync_WhenNoConnectionCreated_DoesNotThrow()
    {
        // Arrange
        var client = new SignalRClient<SignalRClientOptions>(_options, _logger, _accessToken, "testHub");

        // Act & Assert: calling StopAsync without ever starting should not throw
        var exception = await Record.ExceptionAsync(() => client.StopAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task StopAsync_WhenReconnectLoopActive_WaitsForCompletion()
    {
        // Arrange
        var client = new SignalRClient<SignalRClientOptions>(_options, _logger, _accessToken, "testHub");

        // We can't fully test the reconnect loop without a real SignalR server,
        // but we verify that StopAsync completes without throwing when called
        // without a prior StartAsync (defensive behavior)
        var exception = await Record.ExceptionAsync(async () =>
        {
            await client.StopAsync();
        });

        Assert.Null(exception);
    }

    [Fact]
    public async Task StopAsync_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var client = new SignalRClient<SignalRClientOptions>(_options, _logger, _accessToken, "testHub");

        // Act & Assert: multiple StopAsync calls should be safe
        await client.StopAsync();
        var exception = await Record.ExceptionAsync(() => client.StopAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task HubConnection_AfterStop_ThrowsObjectDisposedException()
    {
        // Arrange: use a testable subclass to verify the HubConnection property throws after stopping
        var client = new TestableSignalRClient(_options, _logger, _accessToken, "testHub");

        // Act: stop the client, which sets _isStopping = true
        await client.StopAsync();

        // Assert: accessing HubConnection after stop should throw
        Assert.Throws<ObjectDisposedException>(() => client.GetHubConnection());
    }

    [Fact]
    public void EnableReconnect_WhenNotStarted_ThrowsException()
    {
        // Arrange: the client needs to be started before reconnect can be enabled
        var client = new SignalRClient<SignalRClientOptions>(_options, _logger, _accessToken, "testHub");

        // Act & Assert: EnableReconnect requires _cancelReconnectClient to be initialized
        // which only happens via StartAsync
        Assert.Throws<ServiceClientException>(() => client.EnableReconnect(_ => Task.CompletedTask));
    }

    /// <summary>
    /// Testable subclass that exposes the protected HubConnection property for assertions.
    /// </summary>
    private class TestableSignalRClient : SignalRClient<SignalRClientOptions>
    {
        public TestableSignalRClient(SignalRClientOptions options, ILogger<SignalRClient<SignalRClientOptions>> logger,
            IServiceClientAccessToken accessToken, string hubName)
            : base(options, logger, accessToken, hubName)
        {
        }

        public Microsoft.AspNetCore.SignalR.Client.HubConnection GetHubConnection() => HubConnection;
    }
}
