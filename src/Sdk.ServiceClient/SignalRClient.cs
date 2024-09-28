using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using NLog;

// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable UnusedMember.Global

namespace Meshmakers.Octo.Sdk.ServiceClient;

/// <summary>
///     Implementation of the SignalR client.
/// </summary>
/// <typeparam name="TOptions">Type of options</typeparam>
public class SignalRClient<TOptions> : ISignalRClient<TOptions> where TOptions : SignalRClientOptions
{
    private readonly string _hubName;
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private HubConnection? _hubConnection;
    private CancellationTokenSource? _cancelReconnectClient;

    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="clientOptions"></param>
    /// <param name="serviceClientAccessToken">The access token management object</param>
    /// <param name="hubName"></param>
    public SignalRClient(IOptions<TOptions> clientOptions,
        IServiceClientAccessToken serviceClientAccessToken, string hubName)
        : this(clientOptions.Value, serviceClientAccessToken, hubName)
    {
    }

    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="clientOptions"></param>
    /// <param name="serviceClientAccessToken">The access token management object</param>
    /// <param name="hubName">Name of hub name.</param>
    public SignalRClient(TOptions clientOptions,
        IServiceClientAccessToken serviceClientAccessToken, string hubName)
    {
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
        get { return _hubConnection ??= CreateHubConnection(); }
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
        if (!IsAlive)
        {
            throw ServiceClientException.NotConnected();
        }

        if (_cancelReconnectClient == null)
        {
            throw ServiceClientException.ReconnectAlreadyEnabled();
        }

        _cancelReconnectClient = new CancellationTokenSource();
        
        HubConnection.Closed += async _ =>
        {
            _logger.Info("SignalR connection closed, trying to reconnect");
            while (!_cancelReconnectClient.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(new Random().Next(0, 5) * 1000);

                    if (HubConnection.State == HubConnectionState.Disconnected)
                    {
                        _logger.Info("Starting SignalR client...");
                        await HubConnection.StartAsync();
                    }

                    _logger.Info("SignalR connection started, calling reconnect function");
                    await onReconnectFunction(true);
                    _logger.Info("SignalR connection sucessfully restored");
                    break;
                }
                catch (IOException)
                {
                    _logger.Warn("Input/Ouptut error during reconnect to SignalR hub {HubName}. Trying again..", _hubName);
                }
                catch (HubException)
                {
                    _logger.Warn("Hub returned common error during reconnect to SignalR hub {HubName}. Trying again...", _hubName);
                }
                catch (Exception)
                {
                    _logger.Warn("Common error during reconnect to SignalR hub {HubName}. Trying again..", _hubName);
                }
            }
        };
    }

    /// <inheritdoc />
    public async Task StartAsync(Func<bool, Task> onReconnectFunction, CancellationToken stoppingToken)
    {

        _cancelReconnectClient = new CancellationTokenSource();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (HubConnection.State == HubConnectionState.Disconnected)
                {
                    _logger.Info("Starting SignalR client...");
                    await HubConnection.StartAsync(stoppingToken);
                }
                _logger.Info("SignalR connection started, calling connect function");
                await onReconnectFunction(false);
                _logger.Info("SignalR connection sucessfully established");
                break;
            }
            catch (IOException)
            {
                _logger.Warn("Input/Ouptut error during connect to SignalR hub {HubName}. Trying again..", _hubName);
            }
            catch (HubException)
            {
                _logger.Warn("Hub returned common error during connect to SignalR hub {HubName}. Trying again...", _hubName);
            }
            catch (Exception)
            {
                _logger.Warn("Common error during connect to SignalR hub {HubName}. Trying again..", _hubName);
            }
            await Task.Delay(new Random().Next(0, 5) * 1000, stoppingToken);
        }

        _logger.Info("SignalR client started. ConnectionId: {ConnectionId}", HubConnection.ConnectionId);
    }

    /// <inheritdoc />
    public async Task StopAsync()
    {
        _logger.Info("Stopping SignalR client...");

        if (_cancelReconnectClient != null)
        {
#if NETSTANDARD2_0
            _cancelReconnectClient.Cancel();
#else
            await _cancelReconnectClient.CancelAsync();
#endif
        }
        await HubConnection.StopAsync();

        _logger.Info("SignalR client stopped.");
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