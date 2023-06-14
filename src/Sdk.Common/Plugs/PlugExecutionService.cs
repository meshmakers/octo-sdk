using System;
using System.Threading;
using System.Threading.Tasks;
using Meshmakers.Octo.Common.Shared;
using Meshmakers.Octo.Communication.Plugs.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Plugs.Contracts.Hubs;
using Meshmakers.Octo.Sdk.ServiceClient.PlugControllerServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NLog;

namespace Meshmakers.Octo.Sdk.Common.Plugs;

internal class PlugExecutionService : BackgroundService, IPlugHubCallbacks
{
    private readonly IPlugService _plugService;
    private readonly IPlugHubCallbackService _plugHubCallbackService;
    private readonly IOptions<PlugOptions> _plugOptions;
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly IPlugControllerClient _controllerClient;

    public PlugExecutionService(IPlugControllerClient plugControllerClient,
        IOptions<PlugOptions> plugOptions, IPlugService plugService, IPlugHubCallbackService plugHubCallbackService)
    {
        _plugService = plugService;
        _plugHubCallbackService = plugHubCallbackService;
        _controllerClient = plugControllerClient;
        _plugOptions = plugOptions;
        
        _plugHubCallbackService.RegisterCallback(this);
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

            await _plugService.StartupAsync(new PlugStartup(tenantId, configuration), stoppingToken);

            stoppingToken.WaitHandle.WaitOne();
        }
        catch (Exception e)
        {
            _logger.Error(e, "Error during initialization of plug runner");
        }
        finally
        {
            try
            {
                await _plugService.ShutdownAsync(stoppingToken);

                await _controllerClient.StopAsync();
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error during deinitialization of plug runner");
            }
        }
    }

    private async Task<PlugConfigurationDto?> StartCommunicationAsync(CancellationToken stoppingToken)
    {
        _logger.Info("Starting plug...");
        _logger.Info("Connecting to Plug Hub at '{PlugControllerServicesUri}'",
            _plugOptions.Value.PlugControllerServicesUri);
        _logger.Info("TenantId '{TenantId}', plugId '{PlugId}'",
            _plugOptions.Value.TenantId, _plugOptions.Value.PlugId);

        if (_plugOptions.Value.PlugId == null)
        {
            _logger.Error("PlugId is null");
            return null;
        }

        await _controllerClient.StartAsync();
        _logger.Info("Connected to plug hub");

        if (stoppingToken.IsCancellationRequested)
        {
            await _controllerClient.StopAsync();
            return null;
        }

        _logger.Info("Registering at plug hub");
        var configuration = await _controllerClient.RegisterPlugAsync(OctoObjectId.Parse(_plugOptions.Value.PlugId));
        _logger.Info("Registration successfull");

        return configuration;
    }

    public Task PlugConfigurationUpdatedAsync(string tenantId, PlugConfigurationDto plugConfiguration)
    {
        return Task.CompletedTask;
    }
}