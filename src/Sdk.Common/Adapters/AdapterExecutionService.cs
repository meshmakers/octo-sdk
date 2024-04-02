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
public class AdapterExecutionService : IHostedService, IAdapterHubCallbacks
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
        IOptions<AdapterOptions> adapterOptions, IAdapterService adapterService,
        IAdapterHubCallbackService adapterHubCallbackService)
    {
        _adapterService = adapterService;
        _hubClient = adapterHubClient;
        _adapterOptions = adapterOptions;

        adapterHubCallbackService.RegisterCallback(this);
    }

    /// <inheritdoc />
    public async Task AdapterConfigurationUpdatedAsync(string tenantId, AdapterConfigurationDto adapterConfiguration)
    {
        var cancellationToken = new CancellationToken();
        await _adapterService.ShutdownAsync(new AdapterShutdown { TenantId = tenantId }, cancellationToken);
        await _adapterService.StartupAsync(
            new AdapterStartup
            {
                TenantId = tenantId,
                Configuration = adapterConfiguration
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var configuration = await StartCommunicationAsync(cancellationToken);
            if (configuration == null)
            {
                return;
            }

            var tenantId = _adapterOptions.Value.TenantId;
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return;
            }

            await _adapterService.StartupAsync(
                new AdapterStartup
                    { TenantId = tenantId, Configuration = configuration },
                cancellationToken);
        }
        catch (Exception e)
        {
            _logger.Error(e, "Error during initialization of adapter execution service");
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = _adapterOptions.Value.TenantId;
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return;
            }

            await _adapterService.ShutdownAsync(new AdapterShutdown { TenantId = tenantId }, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_adapterOptions.Value.AdapterRtId))
            {
                var rtEntityId = GetAdapterRtEntityId();
                if (rtEntityId != null)
                {
                    await _hubClient.UnRegisterAdapterAsync(rtEntityId.Value);
                }
            }

            await _hubClient.StopAsync();
        }
        catch (Exception e)
        {
            _logger.Error(e, "Error during deinitialization of adapter execution service");
        }
    }

    private RtEntityId? GetAdapterRtEntityId()
    {
        if (!string.IsNullOrWhiteSpace(_adapterOptions.Value.AdapterRtId) &&
            !string.IsNullOrWhiteSpace(_adapterOptions.Value.AdapterCkTypeId))
        {
            var adapterRtId = OctoObjectId.Parse(_adapterOptions.Value.AdapterRtId);
            var adapterCkId = new CkId<CkTypeId>(_adapterOptions.Value.AdapterCkTypeId);
            var rtEntityId = new RtEntityId(adapterCkId, adapterRtId);
            return rtEntityId;
        }

        return null;
    }

    private async Task<AdapterConfigurationDto?> StartCommunicationAsync(CancellationToken stoppingToken)
    {
        _logger.Info("Starting adapter...");
        _logger.Info("Connecting to adapter hub at {CommunicationControllerServicesUri}",
            _adapterOptions.Value.CommunicationControllerServicesUri);
        _logger.Info("TenantId {TenantId}, AdapterRtId {AdapterRtId}",
            _adapterOptions.Value.TenantId, _adapterOptions.Value.AdapterRtId);

        if (_adapterOptions.Value.AdapterRtId == null)
        {
            _logger.Error("AdapterRtId is null");
            return null;
        }

        var rtEntityId = GetAdapterRtEntityId();
        if (rtEntityId == null)
        {
            _logger.Error("Options missing settings for AdapterRtId and AdapterCkTypeId");
            return null;
        }

        await _hubClient.StartAsync(stoppingToken);
        _logger.Info("Connected to adapter hub");

        if (stoppingToken.IsCancellationRequested)
        {
            await _hubClient.StopAsync();
            return null;
        }

        _logger.Info("Registering at adapter hub");

        var configuration =
            await _hubClient.RegisterAdapterAsync(rtEntityId.Value);
        _logger.Info("Registration successfull");

        _logger.Info("Enabling automatic reconnect");
        _hubClient.EnableReconnect();

        return configuration;
    }
}