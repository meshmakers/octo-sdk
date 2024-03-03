using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Contracts.Hubs;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NLog;

namespace Meshmakers.Octo.Sdk.Common.Adapters;

/// <summary>
/// Background service for the execution of an adapter.
/// </summary>
public class AdapterExecutionService : BackgroundService, IAdapterHubCallbacks
{
    private readonly IAdapterHubClient _hubClient;
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly IOptions<AdapterOptions> _adapterOptions;
    private readonly IAdapterService _adapterService;

    /// <summary>
    /// Creates a new instance of <see cref="AdapterExecutionService"/>.
    /// </summary>
    /// <param name="adapterHubClient"></param>
    /// <param name="adapterOptions"></param>
    /// <param name="adapterService"></param>
    /// <param name="adapterHubCallbackService"></param>
    public AdapterExecutionService(IAdapterHubClient adapterHubClient,
        IOptions<AdapterOptions> adapterOptions, IAdapterService adapterService, IAdapterHubCallbackService adapterHubCallbackService)
    {
        _adapterService = adapterService;
        _hubClient = adapterHubClient;
        _adapterOptions = adapterOptions;

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

            var tenantId = _adapterOptions.Value.TenantId;
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return;
            }

            await _adapterService.StartupAsync(new AdapterStartup { TenantId = tenantId, Configuration = configuration }, stoppingToken);

            stoppingToken.WaitHandle.WaitOne();
        }
        catch (Exception e)
        {
            _logger.Error(e, "Error during initialization of adapter execution service");
        }
        finally
        {
            try
            {
                await _adapterService.ShutdownAsync(stoppingToken);

                if (!string.IsNullOrWhiteSpace(_adapterOptions.Value.AdapterRtId))
                {
                    await _hubClient.UnRegisterAdapterAsync(OctoObjectId.Parse(_adapterOptions.Value.AdapterRtId));
                }

                await _hubClient.StopAsync();
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error during deinitialization of adapter execution service");
            }
        }
    }

    private async Task<AdapterConfigurationDto?> StartCommunicationAsync(CancellationToken stoppingToken)
    {
        _logger.Info("Starting adapter...");
        _logger.Info("Connecting to adapter hub at '{CommunicationControllerServicesUri}'",
            _adapterOptions.Value.CommunicationControllerServicesUri);
        _logger.Info("TenantId '{TenantId}', AdapterRtId '{AdapterRtId}'",
            _adapterOptions.Value.TenantId, _adapterOptions.Value.AdapterRtId);

        if (_adapterOptions.Value.AdapterRtId == null)
        {
            _logger.Error("AdapterRtId is null");
            return null;
        }

        await _hubClient.StartAsync();
        _logger.Info("Connected to adapter hub");

        if (stoppingToken.IsCancellationRequested)
        {
            await _hubClient.StopAsync();
            return null;
        }

        _logger.Info("Registering at adapter hub");
        var configuration = await _hubClient.RegisterAdapterAsync(OctoObjectId.Parse(_adapterOptions.Value.AdapterRtId));
        _logger.Info("Registration successfull");

        return configuration;
    }
}