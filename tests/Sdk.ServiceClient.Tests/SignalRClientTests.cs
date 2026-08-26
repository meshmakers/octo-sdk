using System.Reflection;
using Meshmakers.Octo.Sdk.ServiceClient;
using Microsoft.AspNetCore.SignalR.Client;
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

    [Fact]
    public void CreateHubConnection_InvokesRegisterServerCallbacks_ForEveryConnection()
    {
        // Regression guard (adapter/operator stale-callback bug): server-to-client handlers
        // must be re-bound on EVERY connection created, not once in the constructor. A full
        // StopAsync/StartAsync cycle builds a fresh connection; if handlers were only bound in
        // the constructor, server pushes (e.g. AdapterConfigurationUpdatedAsync pipeline deploys)
        // were silently dropped until the process restarted.
        var client = new CountingSignalRClient(_options, _logger, _accessToken, "testHub");
        var createHubConnection = typeof(SignalRClient<SignalRClientOptions>)
            .GetMethod("CreateHubConnection", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(createHubConnection);

        // Simulate the initial connection plus a post-reconnect rebuild.
        createHubConnection!.Invoke(client, null);
        createHubConnection.Invoke(client, null);

        Assert.Equal(2, client.RegisterCallCount);
    }

    [Fact]
    public async Task StartReconnectLoopIfIdle_WhileLoopActive_DoesNotStartSecondLoop()
    {
        // Regression guard (AB#4805): the Closed event, the watchdog and EnableReconnect may all
        // race to start the reconnect loop — only ONE loop must ever run.
        var client = new SignalRClient<SignalRClientOptions>(_options, _logger, _accessToken, "testHub");
        SetPrivateField(client, "_onReconnectFunction", (Func<bool, Task>)(_ => Task.CompletedTask));
        SetPrivateField(client, "_cancelReconnectClient", new CancellationTokenSource());

        var firstLoop = InvokePrivate<Task>(client, "StartReconnectLoopIfIdle", "test-first");
        var secondCall = InvokePrivate<Task>(client, "StartReconnectLoopIfIdle", "test-second");

        // The second call must be rejected immediately (completed no-op task) while the first
        // loop is still running (it retries against a non-existing server until stopped).
        Assert.True(secondCall.IsCompleted);
        Assert.False(firstLoop.IsCompleted);
        Assert.Equal(1, GetPrivateField<int>(client, "_reconnectLoopActive"));

        await client.StopAsync();
        Assert.Equal(0, GetPrivateField<int>(client, "_reconnectLoopActive"));
    }

    [Fact]
    public async Task WatchdogTick_WhenConnectionNotConnectedAndIdle_StartsReconnectLoop()
    {
        // Regression guard (AB#4805): a connection that is dead (or stuck in a
        // non-Connected state) without a running reconnect loop must be picked up
        // by the watchdog.
        var client = new SignalRClient<SignalRClientOptions>(_options, _logger, _accessToken, "testHub");
        SetPrivateField(client, "_onReconnectFunction", (Func<bool, Task>)(_ => Task.CompletedTask));
        SetPrivateField(client, "_cancelReconnectClient", new CancellationTokenSource());
        // Create a real (disconnected) connection instance for the watchdog to inspect.
        var connection = InvokePrivate<object>(client, "CreateHubConnection");
        SetPrivateField(client, "_hubConnection", connection);

        InvokePrivateVoid(client, "WatchdogTick");

        Assert.Equal(1, GetPrivateField<int>(client, "_reconnectLoopActive"));

        await client.StopAsync();
    }

    [Fact]
    public async Task OnConnectionClosed_DuringInitialStart_DoesNotStartLoop()
    {
        // While the initial start loop runs it owns the connect retries — a Closed event in that
        // window must not spawn a competing reconnect loop.
        var client = new SignalRClient<SignalRClientOptions>(_options, _logger, _accessToken, "testHub");
        SetPrivateField(client, "_onReconnectFunction", (Func<bool, Task>)(_ => Task.CompletedTask));
        SetPrivateField(client, "_cancelReconnectClient", new CancellationTokenSource());
        SetPrivateField(client, "_initialStartInProgress", true);

        var result = InvokePrivate<Task>(client, "OnConnectionClosedAsync");

        Assert.True(result.IsCompleted);
        Assert.Equal(0, GetPrivateField<int>(client, "_reconnectLoopActive"));

        await client.StopAsync();
    }

    [Fact]
    public async Task OnConnectionClosed_AfterStart_StartsReconnectLoop()
    {
        // Regression guard (AB#4805): after the initial start completed, a Closed event must
        // start the reconnect loop — including on connections created AFTER a restart cycle
        // (the handler is attached per-connection in CreateHubConnection, not in EnableReconnect).
        var client = new SignalRClient<SignalRClientOptions>(_options, _logger, _accessToken, "testHub");
        SetPrivateField(client, "_onReconnectFunction", (Func<bool, Task>)(_ => Task.CompletedTask));
        SetPrivateField(client, "_cancelReconnectClient", new CancellationTokenSource());

        var result = InvokePrivate<Task>(client, "OnConnectionClosedAsync");

        Assert.False(result.IsCompleted);
        Assert.Equal(1, GetPrivateField<int>(client, "_reconnectLoopActive"));

        await client.StopAsync();
        Assert.Equal(0, GetPrivateField<int>(client, "_reconnectLoopActive"));
    }

    [Fact]
    public async Task WatchdogTick_DuringFreshInitialStart_DoesNotForceStopOrStartLoop()
    {
        // While the initial start loop is making progress the watchdog must stay out of the way.
        var client = new SignalRClient<SignalRClientOptions>(_options, _logger, _accessToken, "testHub");
        SetPrivateField(client, "_onReconnectFunction", (Func<bool, Task>)(_ => Task.CompletedTask));
        SetPrivateField(client, "_cancelReconnectClient", new CancellationTokenSource());
        SetPrivateField(client, "_initialStartInProgress", true);
        SetPrivateField(client, "_initialStartLastProgressUtc", DateTime.UtcNow);
        var connection = InvokePrivate<object>(client, "CreateHubConnection");
        SetPrivateField(client, "_hubConnection", connection);

        InvokePrivateVoid(client, "WatchdogTick");

        Assert.Equal(0, GetPrivateField<int>(client, "_reconnectLoopActive"));
        AssertWarningLogged(expected: false);

        await client.StopAsync();
    }

    [Fact]
    public async Task WatchdogTick_DuringStalledInitialStart_WarnsAndDoesNotStartCompetingLoop()
    {
        // Regression guard (AB#4876): a start loop stuck inside an unbounded await made zero
        // progress for days while the watchdog stayed silent. A stalled loop must be surfaced
        // (warning + forced connection stop) — but never by spawning a competing loop.
        var client = new SignalRClient<SignalRClientOptions>(_options, _logger, _accessToken, "testHub")
        {
            LoopStallTimeout = TimeSpan.FromMilliseconds(50)
        };
        SetPrivateField(client, "_onReconnectFunction", (Func<bool, Task>)(_ => Task.CompletedTask));
        SetPrivateField(client, "_cancelReconnectClient", new CancellationTokenSource());
        SetPrivateField(client, "_initialStartInProgress", true);
        SetPrivateField(client, "_initialStartLastProgressUtc", DateTime.UtcNow.AddMinutes(-1));
        var connection = InvokePrivate<object>(client, "CreateHubConnection");
        SetPrivateField(client, "_hubConnection", connection);

        InvokePrivateVoid(client, "WatchdogTick");

        Assert.Equal(0, GetPrivateField<int>(client, "_reconnectLoopActive"));
        AssertWarningLogged(expected: true);

        await client.StopAsync();
    }

    [Fact]
    public async Task WatchdogTick_WhenReconnectLoopStalled_Warns()
    {
        // Regression guard (AB#4876): a hung reconnect loop holds the single-loop guard and used
        // to silence the watchdog forever. A stalled loop must be surfaced.
        var client = new SignalRClient<SignalRClientOptions>(_options, _logger, _accessToken, "testHub")
        {
            LoopStallTimeout = TimeSpan.FromMilliseconds(50)
        };
        SetPrivateField(client, "_onReconnectFunction", (Func<bool, Task>)(_ => Task.CompletedTask));
        SetPrivateField(client, "_cancelReconnectClient", new CancellationTokenSource());
        SetPrivateField(client, "_reconnectLoopActive", 1);
        SetPrivateField(client, "_reconnectLoopLastProgressUtc", DateTime.UtcNow.AddMinutes(-1));
        var connection = InvokePrivate<object>(client, "CreateHubConnection");
        SetPrivateField(client, "_hubConnection", connection);

        InvokePrivateVoid(client, "WatchdogTick");

        AssertWarningLogged(expected: true);

        SetPrivateField(client, "_reconnectLoopActive", 0);
        await client.StopAsync();
    }

    [Fact]
    public async Task StartAsync_WithCancelledToken_StillArmsWatchdog()
    {
        // Regression guard (AB#4876): the watchdog used to be armed only AFTER the start loop
        // completed successfully — a start that exited via cancellation (or hung) left the client
        // without any watchdog, so a later connection loss had no recovery path.
        var client = new SignalRClient<SignalRClientOptions>(_options, _logger, _accessToken, "testHub");

        await client.StartAsync(_ => Task.CompletedTask, new CancellationToken(canceled: true));

        Assert.NotNull(GetPrivateField<object?>(client, "_watchdogTimer"));

        await client.StopAsync();
    }

    private void AssertWarningLogged(bool expected)
    {
        var warningCalls = Fake.GetCalls(_logger).Count(call =>
            call.Method.Name == nameof(ILogger.Log) &&
            call.Arguments.Count > 0 &&
            call.Arguments[0] is LogLevel level &&
            level == LogLevel.Warning);
        if (expected)
        {
            Assert.True(warningCalls > 0, "Expected at least one warning log, but none was written.");
        }
        else
        {
            Assert.Equal(0, warningCalls);
        }
    }

    private static void SetPrivateField(object target, string name, object? value)
    {
        var field = typeof(SignalRClient<SignalRClientOptions>)
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        field!.SetValue(target, value);
    }

    private static T GetPrivateField<T>(object target, string name)
    {
        var field = typeof(SignalRClient<SignalRClientOptions>)
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        return (T)field!.GetValue(target)!;
    }

    private static T InvokePrivate<T>(object target, string name, params object?[] args)
    {
        var method = typeof(SignalRClient<SignalRClientOptions>)
            .GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        return (T)method!.Invoke(target, args)!;
    }

    private static void InvokePrivateVoid(object target, string name, params object?[] args)
    {
        var method = typeof(SignalRClient<SignalRClientOptions>)
            .GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        method!.Invoke(target, args);
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

        public HubConnection GetHubConnection() => HubConnection;
    }

    /// <summary>
    /// Testable subclass that counts how often the server-callback registration hook fires.
    /// </summary>
    private class CountingSignalRClient : SignalRClient<SignalRClientOptions>
    {
        public CountingSignalRClient(SignalRClientOptions options, ILogger<SignalRClient<SignalRClientOptions>> logger,
            IServiceClientAccessToken accessToken, string hubName)
            : base(options, logger, accessToken, hubName)
        {
        }

        public int RegisterCallCount { get; private set; }

        protected override void RegisterServerCallbacks(HubConnection hubConnection)
        {
            RegisterCallCount++;
        }
    }
}
