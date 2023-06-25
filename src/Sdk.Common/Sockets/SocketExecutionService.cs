using System;
using System.Threading;
using System.Threading.Tasks;
using Meshmakers.Octo.Common.Shared;
using Meshmakers.Octo.Communication.Sockets.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Sockets.Contracts.Hubs;
using Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NLog;

namespace Meshmakers.Octo.Sdk.Common.Sockets;

public class SocketExecutionService: BackgroundService, ISocketHubCallbacks
{
    private readonly ISocketHubClient _hubClient;
    private readonly IOptions<SocketOptions> _socketOptions;
    private readonly ISocketService _socketService;
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public SocketExecutionService(ISocketHubClient socketHubClient,
        IOptions<SocketOptions> socketOptions, ISocketService socketService, ISocketHubCallbackService socketHubCallbackService)
    {
        _socketService = socketService;
        _hubClient = socketHubClient;
        _socketOptions = socketOptions;
        
        socketHubCallbackService.RegisterCallback(this);
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var configuration = await StartCommunicationAsync(stoppingToken);
            if (configuration == null)
                return;

            var tenantId = _socketOptions.Value.TenantId;
            if (string.IsNullOrWhiteSpace(tenantId))
                return;

            await _socketService.StartupAsync(new SocketStartup {TenantId = tenantId, Configuration = configuration}, stoppingToken);

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
                
                if (_socketOptions.Value.SocketRtId != null)
                {
                    _logger.Warn("SocketRtId is null");
                    await _hubClient.UnRegisterSocketAsync(OctoObjectId.Parse(_socketOptions.Value.SocketRtId));
                }


                await _hubClient.StopAsync();
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error during deinitialization of socket execution service");
            }
        }
    }
    
    private async Task<SocketConfigurationDto?> StartCommunicationAsync(CancellationToken stoppingToken)
    {
        _logger.Info("Starting socket...");
        _logger.Info("Connecting to socket hub at '{CommunicationControllerServicesUri}'",
            _socketOptions.Value.CommunicationControllerServicesUri);
        _logger.Info("TenantId '{TenantId}', SocketRtId '{SocketRtId}'",
            _socketOptions.Value.TenantId, _socketOptions.Value.SocketRtId);

        if (_socketOptions.Value.SocketRtId == null)
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
        var configuration = await _hubClient.RegisterSocketAsync(OctoObjectId.Parse(_socketOptions.Value.SocketRtId));
        _logger.Info("Registration successfull");

        return configuration;
    }

    public Task SocketConfigurationUpdatedAsync(string tenantId, SocketConfigurationDto socketConfiguration)
    {
        return Task.CompletedTask;
    }
}