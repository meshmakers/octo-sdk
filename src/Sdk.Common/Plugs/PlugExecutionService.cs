using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Contracts.Hubs;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NLog;

namespace Meshmakers.Octo.Sdk.Common.Plugs;

internal class PlugExecutionService : BackgroundService, IAdapterHubCallbacks
{
    private readonly IAdapterHubClient _hubClient;
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly IOptions<PlugOptions> _plugOptions;
    private readonly IPlugService _plugService;

    public PlugExecutionService(IAdapterHubClient adapterHubClient,
        IOptions<PlugOptions> plugOptions, IPlugService plugService, IAdapterHubCallbackService adapterHubCallbackService)
    {
        _plugService = plugService;
        _hubClient = adapterHubClient;
        _plugOptions = plugOptions;

        adapterHubCallbackService.RegisterCallback(this);
    }

    public Task AdapterConfigurationUpdatedAsync(string tenantId, AdapterConfigurationDto adapterConfiguration)
    {
        return Task.CompletedTask;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var configuration = await StartCommunicationAsync(stoppingToken);
            if (configuration == null)
            {
                return;
            }

            var tenantId = _plugOptions.Value.TenantId;
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return;
            }

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

                if (_plugOptions.Value.AdapterRtId != null)
                {
                    _logger.Warn("AdapterRtId is null");
                    await _hubClient.UnRegisterAdapterAsync(OctoObjectId.Parse(_plugOptions.Value.AdapterRtId));
                }

                await _hubClient.StopAsync();
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error during deinitialization of plug execution service");
            }
        }
    }

    private async Task<AdapterConfigurationDto?> StartCommunicationAsync(CancellationToken stoppingToken)
    {
        _logger.Info("Starting plug...");
        _logger.Info("Connecting to Plug Hub at '{CommunicationControllerServicesUri}'",
            _plugOptions.Value.CommunicationControllerServicesUri);
        _logger.Info("TenantId '{TenantId}', AdapterRtId '{AdapterRtId}'",
            _plugOptions.Value.TenantId, _plugOptions.Value.AdapterRtId);

        if (_plugOptions.Value.AdapterRtId == null)
        {
            _logger.Error("AdapterRtId is null");
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
        var configuration = await _hubClient.RegisterAdapterAsync(OctoObjectId.Parse(_plugOptions.Value.AdapterRtId));
        _logger.Info("Registration successfull");

        return configuration;
    }
}