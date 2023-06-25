using System;
using System.Threading;
using System.Threading.Tasks;
using Meshmakers.Octo.Common.Shared;
using Meshmakers.Octo.Communication.Plugs.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Plugs.Contracts.Hubs;
using Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NLog;

namespace Meshmakers.Octo.Sdk.Common.Plugs;

internal class PlugExecutionService : BackgroundService, IPlugHubCallbacks
{
    private readonly IPlugService _plugService;
    private readonly IOptions<PlugOptions> _plugOptions;
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly IPlugHubClient _hubClient;

    public PlugExecutionService(IPlugHubClient plugHubClient,
        IOptions<PlugOptions> plugOptions, IPlugService plugService, IPlugHubCallbackService plugHubCallbackService)
    {
        _plugService = plugService;
        _hubClient = plugHubClient;
        _plugOptions = plugOptions;

        plugHubCallbackService.RegisterCallback(this);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var configuration = await StartCommunicationAsync(stoppingToken);
            if (configuration == null)
                return;

            var tenantId = _plugOptions.Value.TenantId;
            if (string.IsNullOrWhiteSpace(tenantId))
                return;

            await _plugService.StartupAsync(new PlugStartup { TenantId = tenantId, Configuration = configuration }, stoppingToken);

            stoppingToken.WaitHandle.WaitOne();
        }
        catch (Exception e)
        {
            _logger.Error(e, "Error during initialization of plug execution service");
        }
        finally
        {
            try
            {
                await _plugService.ShutdownAsync(stoppingToken);

                if (_plugOptions.Value.PlugRtId != null)
                {
                    _logger.Warn("PlugRtId is null");
                    await _hubClient.UnRegisterPlugAsync(OctoObjectId.Parse(_plugOptions.Value.PlugRtId));
                }

                await _hubClient.StopAsync();
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error during deinitialization of plug execution service");
            }
        }
    }

    private async Task<PlugConfigurationDto?> StartCommunicationAsync(CancellationToken stoppingToken)
    {
        _logger.Info("Starting plug...");
        _logger.Info("Connecting to Plug Hub at '{CommunicationControllerServicesUri}'",
            _plugOptions.Value.CommunicationControllerServicesUri);
        _logger.Info("TenantId '{TenantId}', PlugRtId '{PlugRtId}'",
            _plugOptions.Value.TenantId, _plugOptions.Value.PlugRtId);

        if (_plugOptions.Value.PlugRtId == null)
        {
            _logger.Error("PlugRtId is null");
            return null;
        }

        await _hubClient.StartAsync();
        _logger.Info("Connected to plug hub");

        if (stoppingToken.IsCancellationRequested)
        {
            await _hubClient.StopAsync();
            return null;
        }

        _logger.Info("Registering at plug hub");
        var configuration = await _hubClient.RegisterPlugAsync(OctoObjectId.Parse(_plugOptions.Value.PlugRtId));
        _logger.Info("Registration successfull");

        return configuration;
    }

    public Task PlugConfigurationUpdatedAsync(string tenantId, PlugConfigurationDto plugConfiguration)
    {
        return Task.CompletedTask;
    }
}