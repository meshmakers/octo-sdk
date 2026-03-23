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
    private readonly ILogger<SignalRClient<TOptions>> _logger;
    private readonly string _hubName;
    private HubConnection? _hubConnection;
    private CancellationTokenSource? _cancelReconnectClient;
    private volatile bool _isStopping;
    private Task? _activeReconnectLoopTask;

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

        HubConnection.Closed += _ =>
        {
            if (_isStopping || _cancelReconnectClient == null || _cancelReconnectClient.IsCancellationRequested)
            {
                _logger.LogInformation("SignalR connection closed, reconnect is disabled");
                return Task.CompletedTask;
            }

            _activeReconnectLoopTask = ReconnectLoopAsync(onReconnectFunction);
            return _activeReconnectLoopTask;
        };

        // If the connection is already disconnected, start reconnecting immediately
        if (!IsAlive)
        {
            _logger.LogWarning("Connection is not active when enabling reconnect, starting reconnect loop immediately");
            _activeReconnectLoopTask = ReconnectLoopAsync(onReconnectFunction);
        }
    }

    /// <inheritdoc />
    public async Task StartAsync(Func<bool, Task> onReconnectFunction, CancellationToken stoppingToken)
    {
        _isStopping = false;
        _activeReconnectLoopTask = null;
        _cancelReconnectClient = new CancellationTokenSource();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (HubConnection.State == HubConnectionState.Disconnected)
                {
                    _logger.LogInformation("Starting SignalR client...");
                    await HubConnection.StartAsync(stoppingToken);
                }
                _logger.LogInformation("SignalR connection started, calling connect function");
                await onReconnectFunction(false);
                _logger.LogInformation("SignalR connection successfully established");
                break;
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

        _logger.LogInformation("SignalR client started. ConnectionId: {ConnectionId}", HubConnection.ConnectionId);
    }

    /// <inheritdoc />
    public async Task StopAsync()
    {
        _logger.LogInformation("Stopping SignalR client...");

        _isStopping = true;

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

    private async Task ReconnectLoopAsync(Func<bool, Task> onReconnectFunction)
    {
        _logger.LogInformation("SignalR connection closed, trying to reconnect");
        while (_cancelReconnectClient != null && !_cancelReconnectClient.IsCancellationRequested && !_isStopping)
        {
            try
            {
                await Task.Delay(new Random().Next(0, 5) * 1000);

                if (_isStopping || (_cancelReconnectClient?.IsCancellationRequested ?? true))
                {
                    _logger.LogInformation("Reconnect cancelled during shutdown");
                    break;
                }

                if (HubConnection.State == HubConnectionState.Disconnected)
                {
                    _logger.LogInformation("Starting SignalR client...");
                    await HubConnection.StartAsync();
                }

                _logger.LogInformation("SignalR connection started, calling reconnect function");
                await onReconnectFunction(true);
                _logger.LogInformation("SignalR connection successfully restored");
                break;
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

    private HubConnection CreateHubConnection()
    {
        if (string.IsNullOrWhiteSpace(Options.EndpointUri))
        {
            throw new ServiceConfigurationMissingException("Communication Controller service URI is not configured.");
        }

        if (string.IsNullOrWhiteSpace(Options.TenantId))
        {
            throw new ServiceConfigurationMissingException("TenantId is not configured.");
        }

        ServiceUri = new Uri(Options.EndpointUri).Append(Options.TenantId!).Append(_hubName);

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

        return hubConnection;
    }
}