using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Contracts.Hubs;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NLog;

namespace Meshmakers.Octo.Sdk.Common.Sockets;

/// <summary>
///     Executes the socket.
/// </summary>
public class SocketExecutionService : BackgroundService, IAdapterHubCallbacks
{
    private readonly IAdapterHubClient _hubClient;
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly IOptions<SocketOptions> _socketOptions;
    private readonly ISocketService _socketService;

    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="socketHubClient">SignalR client to communication with backend</param>
    /// <param name="socketOptions">Options</param>
    /// <param name="socketService">The custom implemented socket service</param>
    /// <param name="adapterHubCallbackService">Interface of callback service that is used to handle updates of the backend</param>
    public SocketExecutionService(IAdapterHubClient socketHubClient,
        IOptions<SocketOptions> socketOptions, ISocketService socketService, IAdapterHubCallbackService adapterHubCallbackService)
    {
        _socketService = socketService;
        _hubClient = socketHubClient;
        _socketOptions = socketOptions;

        adapterHubCallbackService.RegisterCallback(this);
    }

    /// <inheritdoc />
    public Task AdapterConfigurationUpdatedAsync(string tenantId, AdapterConfigurationDto adapterConfiguration)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var configuration = await StartCommunicationAsync(stoppingToken);
            if (configuration == null)
            {
                return;
            }

            var tenantId = _socketOptions.Value.TenantId;
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return;
            }

            await _socketService.StartupAsync(new SocketStartup { TenantId = tenantId, Configuration = configuration }, stoppingToken);

            stoppingToken.WaitHandle.WaitOne();
        }
        catch (Exception e)
        {
            _logger.Error(e, "Error during initialization of socket execution service");
        }
        finally
        {
            try
            {
                await _socketService.ShutdownAsync(stoppingToken);

                if (_socketOptions.Value.AdapterRtId != null)
                {
                    _logger.Warn("SocketRtId is null");
                    await _hubClient.UnRegisterAdapterAsync(OctoObjectId.Parse(_socketOptions.Value.AdapterRtId));
                }


                await _hubClient.StopAsync();
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error during deinitialization of socket execution service");
            }
        }
    }

    private async Task<AdapterConfigurationDto?> StartCommunicationAsync(CancellationToken stoppingToken)
    {
        _logger.Info("Starting socket...");
        _logger.Info("Connecting to socket hub at '{CommunicationControllerServicesUri}'",
            _socketOptions.Value.CommunicationControllerServicesUri);
        _logger.Info("TenantId '{TenantId}', SocketRtId '{SocketRtId}'",
            _socketOptions.Value.TenantId, _socketOptions.Value.AdapterRtId);

        if (_socketOptions.Value.AdapterRtId == null)
        {
            _logger.Error("SocketRtId is null");
            return null;
        }

        await _hubClient.StartAsync();
        _logger.Info("Connected to socket hub");

        if (stoppingToken.IsCancellationRequested)
        {
            await _hubClient.StopAsync();
            return null;
        }

        _logger.Info("Registering at socket hub");
        var configuration = await _hubClient.RegisterAdapterAsync(OctoObjectId.Parse(_socketOptions.Value.AdapterRtId));
        _logger.Info("Registration successfull");

        return configuration;
    }
}