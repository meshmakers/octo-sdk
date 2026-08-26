using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable UnusedMember.Global

namespace Meshmakers.Octo.Sdk.ServiceClient;

/// <summary>
///     Implementation of the SignalR client.
/// </summary>
/// <typeparam name="TOptions">Type of options</typeparam>
public class SignalRClient<TOptions> : ISignalRClient<TOptions> where TOptions : SignalRClientOptions
{
    // A single connection attempt must not hang forever (a half-open TCP connection or a peer
    // that crashed without RST otherwise blocks the reconnect loop indefinitely, AB#4805).
    private static readonly TimeSpan ConnectAttemptTimeout = TimeSpan.FromSeconds(30);

    // Safety net: periodically verify the connection is really active and start the reconnect
    // loop if every event-driven path missed the disconnect (AB#4805).
    private static readonly TimeSpan WatchdogInterval = TimeSpan.FromSeconds(60);

    private readonly ILogger<SignalRClient<TOptions>> _logger;
    private readonly string _hubName;
    private HubConnection? _hubConnection;
    private CancellationTokenSource? _cancelReconnectClient;
    private volatile bool _isStopping;
    private Task? _activeReconnectLoopTask;
    private Func<bool, Task>? _onReconnectFunction;
    private int _reconnectLoopActive;
    private volatile bool _initialStartInProgress;
    private Timer? _watchdogTimer;
    private DateTime _initialStartLastProgressUtc;
    private DateTime _reconnectLoopLastProgressUtc;

    /// <summary>
    ///     How long the initial start loop or the reconnect loop may sit between iterations before
    ///     the watchdog treats it as stalled and force-stops the connection to fault the stuck
    ///     awaits. A loop stuck inside an unbounded await used to silence the watchdog forever —
    ///     the adapter then sat without a hub connection until a pod restart (AB#4876).
    /// </summary>
    internal TimeSpan LoopStallTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="clientOptions">The client options</param>
    /// <param name="logger">The logger instance</param>
    /// <param name="serviceClientAccessToken">The access token management object</param>
    /// <param name="hubName">Name of hub name.</param>
    public SignalRClient(IOptions<TOptions> clientOptions, ILogger<SignalRClient<TOptions>> logger,
        IServiceClientAccessToken serviceClientAccessToken, string hubName)
        : this(clientOptions.Value, logger, serviceClientAccessToken, hubName)
    {
    }

    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="clientOptions">The client options</param>
    /// <param name="logger">The logger instance</param>
    /// <param name="serviceClientAccessToken">The access token management object</param>
    /// <param name="hubName">Name of hub name.</param>
    public SignalRClient(TOptions clientOptions, ILogger<SignalRClient<TOptions>> logger,
        IServiceClientAccessToken serviceClientAccessToken, string hubName)
    {
        _logger = logger;
        _hubName = hubName;
        ClientAccessToken = serviceClientAccessToken;
        Options = clientOptions;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    /// <summary>
    ///     The hub connection.
    /// </summary>
    protected HubConnection HubConnection
    {
        get
        {
            if (_isStopping)
            {
                throw new ObjectDisposedException(nameof(HubConnection), "The SignalR client is stopping.");
            }

            return _hubConnection ??= CreateHubConnection();
        }
    }

    /// <inheritdoc />
    public bool IsAlive => HubConnection.State != HubConnectionState.Disconnected;

    /// <inheritdoc />
    public IServiceClientAccessToken ClientAccessToken { get; }

    /// <inheritdoc />
    public TOptions Options { get; }

    /// <inheritdoc />
    public Uri? ServiceUri { get; private set; }

    /// <inheritdoc />
    public void EnableReconnect(Func<bool, Task> onReconnectFunction)
    {
        if (_cancelReconnectClient == null)
        {
            throw ServiceClientException.ReconnectAlreadyEnabled();
        }

        _cancelReconnectClient = new CancellationTokenSource();
        _onReconnectFunction = onReconnectFunction;

        // The Closed handler is attached to every connection in CreateHubConnection; here only the
        // case remains where the connection already died (or got stuck) before reconnect was
        // enabled — e.g. the hub went down while the initial registration was still running
        // (AB#4805: that window used to have no reconnect path at all).
        if (HubConnection.State != HubConnectionState.Connected)
        {
            _logger.LogWarning("Connection is not active when enabling reconnect, starting reconnect loop immediately");
            _ = StartReconnectLoopIfIdle("connection not active when enabling reconnect");
        }
    }

    /// <inheritdoc />
    public async Task StartAsync(Func<bool, Task> onReconnectFunction, CancellationToken stoppingToken)
    {
        _isStopping = false;
        _activeReconnectLoopTask = null;
        _onReconnectFunction = onReconnectFunction;
        _cancelReconnectClient = new CancellationTokenSource();
        _initialStartInProgress = true;
        _initialStartLastProgressUtc = DateTime.UtcNow;

        // Arm the watchdog BEFORE the start loop, not after it: a start loop stuck inside an
        // unbounded await (or exited via the cancellation path) otherwise leaves the client
        // without any watchdog at all, and a later connection loss has no recovery (AB#4876).
        StartWatchdog();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _initialStartLastProgressUtc = DateTime.UtcNow;
                try
                {
                    await EnsureConnectionStartedAsync(stoppingToken);
                    _logger.LogInformation("SignalR connection started, calling connect function");
                    await onReconnectFunction(false);

                    // The connect function may swallow its own errors (it reports them out-of-band),
                    // so a normal return is no proof of success — verify the connection is still
                    // active before leaving the start loop (AB#4805).
                    if (HubConnection.State == HubConnectionState.Connected)
                    {
                        _logger.LogInformation("SignalR connection successfully established");
                        break;
                    }

                    _logger.LogWarning(
                        "Connection to SignalR hub {HubName} is no longer active after the connect function completed. Trying again..",
                        _hubName);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Connect to SignalR hub {HubName} cancelled during shutdown", _hubName);
                    return;
                }
                catch (IOException)
                {
                    _logger.LogWarning("Input/Ouptut error during connect to SignalR hub {HubName}. Trying again..", _hubName);
                }
                catch (HubException)
                {
                    _logger.LogWarning("Hub returned common error during connect to SignalR hub {HubName}. Trying again...", _hubName);
                }
                catch (Exception)
                {
                    _logger.LogWarning("Common error during connect to SignalR hub {HubName}. Trying again..", _hubName);
                }
                await Task.Delay(new Random().Next(0, 5) * 1000, stoppingToken);
            }
        }
        finally
        {
            _initialStartInProgress = false;
        }

        _logger.LogInformation("SignalR client started. ConnectionId: {ConnectionId}", HubConnection.ConnectionId);
    }

    /// <inheritdoc />
    public async Task StopAsync()
    {
        _logger.LogInformation("Stopping SignalR client...");

        _isStopping = true;

        _watchdogTimer?.Dispose();
        _watchdogTimer = null;

        if (_cancelReconnectClient != null)
        {
#if NETSTANDARD2_0
            _cancelReconnectClient.Cancel();
#else
            await _cancelReconnectClient.CancelAsync();
#endif
        }

        // Wait for any active reconnect loop to finish before disposing the connection
        if (_activeReconnectLoopTask != null)
        {
            _logger.LogInformation("Waiting for active reconnect loop to complete...");
            try
            {
#if NETSTANDARD2_0
                await Task.WhenAny(_activeReconnectLoopTask, Task.Delay(TimeSpan.FromSeconds(10)));
#else
                await _activeReconnectLoopTask.WaitAsync(TimeSpan.FromSeconds(10));
#endif
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("Timed out waiting for reconnect loop to complete");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while waiting for reconnect loop to complete");
            }

            _activeReconnectLoopTask = null;
        }

        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }

        _logger.LogInformation("SignalR client stopped");
    }

    /// <summary>
    ///     Brings the connection into the Connected state or throws. A connection stuck in a
    ///     non-Disconnected, non-Connected state (Connecting/Reconnecting with a dead transport)
    ///     is force-stopped first — <see cref="HubConnection.StartAsync(CancellationToken)" /> is only
    ///     valid from Disconnected, and without the force-stop such a connection never recovers
    ///     (AB#4805). Every attempt is bounded by <see cref="ConnectAttemptTimeout" />.
    /// </summary>
    private async Task EnsureConnectionStartedAsync(CancellationToken cancellationToken)
    {
        var connection = HubConnection;
        if (connection.State == HubConnectionState.Connected)
        {
            return;
        }

        if (connection.State != HubConnectionState.Disconnected)
        {
            _logger.LogWarning(
                "SignalR connection to hub {HubName} is stuck in state {State}, forcing a stop before restarting",
                _hubName, connection.State);
            // Bound the force-stop too — StopAsync over a dead transport can itself hang, and an
            // unbounded await here freezes the whole (re)connect loop (AB#4876).
            using var stopCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            stopCts.CancelAfter(ConnectAttemptTimeout);
            await connection.StopAsync(stopCts.Token);
        }

        if (connection.State == HubConnectionState.Disconnected)
        {
            _logger.LogInformation("Starting SignalR client...");
            using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            attemptCts.CancelAfter(ConnectAttemptTimeout);
            await connection.StartAsync(attemptCts.Token);
        }
    }

    /// <summary>
    ///     Handles the Closed event of every connection created by <see cref="CreateHubConnection" />.
    ///     Attached at connection creation time (not in <see cref="EnableReconnect" />) so that a
    ///     connection loss during the initial registration window is never left without a reconnect
    ///     path (AB#4805).
    /// </summary>
    private Task OnConnectionClosedAsync()
    {
        if (_isStopping || _cancelReconnectClient == null || _cancelReconnectClient.IsCancellationRequested)
        {
            _logger.LogInformation("SignalR connection closed, reconnect is disabled");
            return Task.CompletedTask;
        }

        if (_initialStartInProgress)
        {
            // The initial start loop is still running and owns the connect retries.
            _logger.LogInformation("SignalR connection closed during initial start, the start loop handles the retry");
            return Task.CompletedTask;
        }

        return StartReconnectLoopIfIdle("connection closed");
    }

    /// <summary>
    ///     Starts the reconnect loop unless one is already running. Callers race freely (Closed
    ///     event, watchdog, EnableReconnect) — the Interlocked guard guarantees a single loop.
    /// </summary>
    private Task StartReconnectLoopIfIdle(string reason)
    {
        var onReconnectFunction = _onReconnectFunction;
        if (onReconnectFunction == null)
        {
            return Task.CompletedTask;
        }

        if (Interlocked.CompareExchange(ref _reconnectLoopActive, 1, 0) != 0)
        {
            return Task.CompletedTask;
        }

        _logger.LogWarning("Starting SignalR reconnect loop for hub {HubName} ({Reason})", _hubName, reason);
        _reconnectLoopLastProgressUtc = DateTime.UtcNow;
        var loopTask = Task.Run(async () =>
        {
            try
            {
                await ReconnectLoopAsync(onReconnectFunction);
            }
            finally
            {
                Interlocked.Exchange(ref _reconnectLoopActive, 0);
            }
        });
        _activeReconnectLoopTask = loopTask;
        return loopTask;
    }

    private void StartWatchdog()
    {
        _watchdogTimer?.Dispose();
        _watchdogTimer = new Timer(_ => WatchdogTick(), null, WatchdogInterval, WatchdogInterval);
    }

    private void WatchdogTick()
    {
        try
        {
            if (_isStopping || _onReconnectFunction == null)
            {
                return;
            }

            var cancelReconnectClient = _cancelReconnectClient;
            if (cancelReconnectClient == null || cancelReconnectClient.IsCancellationRequested)
            {
                return;
            }

            var connection = _hubConnection;
            if (connection == null || connection.State == HubConnectionState.Connected)
            {
                return;
            }

            // The start loop owns the connect retries while it runs — but a loop stuck inside an
            // unbounded await makes zero progress and would otherwise silence the watchdog forever
            // (AB#4876: >4 days without a single reconnect attempt). Force-stop the connection so
            // the stuck awaits fault and the loop resumes; never spawn a competing loop here.
            if (_initialStartInProgress)
            {
                WarnAndForceStopIfStalled(connection, _initialStartLastProgressUtc, "initial start loop");
                return;
            }

            if (Volatile.Read(ref _reconnectLoopActive) == 1)
            {
                WarnAndForceStopIfStalled(connection, _reconnectLoopLastProgressUtc, "reconnect loop");
                return;
            }

            _ = StartReconnectLoopIfIdle($"watchdog found connection in state {connection.State}");
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Error in SignalR connection watchdog for hub {HubName}", _hubName);
        }
    }

    private void WarnAndForceStopIfStalled(HubConnection connection, DateTime lastProgressUtc, string loopName)
    {
        var stalledFor = DateTime.UtcNow - lastProgressUtc;
        if (stalledFor < LoopStallTimeout)
        {
            return;
        }

        _logger.LogWarning(
            "SignalR {LoopName} for hub {HubName} has made no progress for {StalledFor}, forcing a connection stop to unblock it",
            loopName, _hubName, stalledFor);

        _ = Task.Run(async () =>
        {
            try
            {
                using var cts = new CancellationTokenSource(ConnectAttemptTimeout);
                await connection.StopAsync(cts.Token);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Forced stop of SignalR connection to hub {HubName} failed", _hubName);
            }
        });
    }

    private async Task ReconnectLoopAsync(Func<bool, Task> onReconnectFunction)
    {
        _logger.LogInformation("SignalR connection closed, trying to reconnect");
        while (_cancelReconnectClient != null && !_cancelReconnectClient.IsCancellationRequested && !_isStopping)
        {
            _reconnectLoopLastProgressUtc = DateTime.UtcNow;
            try
            {
                await Task.Delay(new Random().Next(1, 5) * 1000);

                if (_isStopping || (_cancelReconnectClient?.IsCancellationRequested ?? true))
                {
                    _logger.LogInformation("Reconnect cancelled during shutdown");
                    break;
                }

                await EnsureConnectionStartedAsync(CancellationToken.None);

                _logger.LogInformation("SignalR connection started, calling reconnect function");
                await onReconnectFunction(true);

                // The reconnect function may swallow its own errors (it reports them out-of-band),
                // so a normal return is no proof of success — verify the connection is really
                // active before leaving the loop. Without this check the loop exited
                // "successfully" over a dead connection and no further reconnect ever happened
                // (AB#4805).
                if (HubConnection.State == HubConnectionState.Connected)
                {
                    _logger.LogInformation("SignalR connection successfully restored");
                    break;
                }

                _logger.LogWarning(
                    "Connection to SignalR hub {HubName} is not active after the reconnect function completed. Trying again..",
                    _hubName);
            }
            catch (ObjectDisposedException)
            {
                _logger.LogInformation("SignalR connection was disposed during reconnect, stopping reconnect loop");
                break;
            }
            catch (IOException)
            {
                _logger.LogWarning("Input/Output error during reconnect to SignalR hub {HubName}. Trying again..", _hubName);
            }
            catch (HubException)
            {
                _logger.LogWarning("Hub returned common error during reconnect to SignalR hub {HubName}. Trying again...", _hubName);
            }
            catch (Exception)
            {
                _logger.LogWarning("Common error during reconnect to SignalR hub {HubName}. Trying again..", _hubName);
            }
        }
    }

    /// <summary>
    /// Builds the service URI for the SignalR hub connection.
    /// Override in subclasses to customize URL construction (e.g., for non-tenant-scoped hubs).
    /// </summary>
    protected virtual Uri BuildServiceUri()
    {
        if (string.IsNullOrWhiteSpace(Options.EndpointUri))
        {
            throw new ServiceConfigurationMissingException("Communication Controller service URI is not configured.");
        }

        if (string.IsNullOrWhiteSpace(Options.TenantId))
        {
            throw new ServiceConfigurationMissingException("TenantId is not configured.");
        }

        return new Uri(Options.EndpointUri).Append(Options.TenantId!).Append(_hubName);
    }

    /// <summary>
    ///     Hook for subclasses to (re-)register their server-to-client callback handlers
    ///     (<c>HubConnection.On&lt;...&gt;(...)</c>) on the supplied connection.
    ///     Called for EVERY connection created by <see cref="CreateHubConnection" /> — the
    ///     initial one and every one built after a <see cref="StopAsync" /> nulls the cached
    ///     connection. Registering the handlers only once in a subclass constructor is a bug:
    ///     a full stop/start (e.g. a PreUpdateTenant-triggered adapter restart) builds a fresh
    ///     <see cref="HubConnection" /> with no handlers, so every server-to-client push is
    ///     silently dropped until the process restarts. Bind to the passed
    ///     <paramref name="hubConnection" />, never the <see cref="HubConnection" /> property
    ///     (that would recurse, as the property is mid-creation here).
    /// </summary>
    /// <param name="hubConnection">The freshly created connection to register handlers on.</param>
    protected virtual void RegisterServerCallbacks(HubConnection hubConnection)
    {
    }

    private HubConnection CreateHubConnection()
    {
        ServiceUri = BuildServiceUri();

        var hubConnection = new HubConnectionBuilder()
            .WithUrl(ServiceUri, options =>
            {
                options.HttpMessageHandlerFactory = message =>
                {
                    if (message is HttpClientHandler clientHandler)
                        // always verify the SSL certificate
                    {
                        clientHandler.ServerCertificateCustomValidationCallback +=
                            (_, _, _, _) => true;
                    }

                    return message;
                };
                // TODO: Handle authentication
                options.Headers["Authorization"] = "Bearer your-access-token";

                // Add optional headers to requests
                foreach (var header in Options.Headers)
                {
                    options.Headers[header.Key] = header.Value;
                }
            })
            .Build();

        // Re-bind server-to-client callbacks on every new connection (not just the first),
        // so they survive a StopAsync/StartAsync cycle. See RegisterServerCallbacks.
        RegisterServerCallbacks(hubConnection);

        // Every connection gets the reconnect Closed handler at creation time — attaching it
        // lazily (the former EnableReconnect subscription) left a window in which a connection
        // loss had no reconnect path (AB#4805).
        hubConnection.Closed += _ => OnConnectionClosedAsync();

        return hubConnection;
    }
}
