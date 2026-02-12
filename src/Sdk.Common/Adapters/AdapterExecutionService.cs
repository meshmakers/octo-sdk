using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Contracts.Hubs;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Sdk.Common.Services;
using Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;
using Microsoft.Extensions.Options;
using NLog;

namespace Meshmakers.Octo.Sdk.Common.Adapters;

/// <summary>
/// Background service for the execution of an adapter.
/// </summary>
public class AdapterExecutionService : IAdapterHubCallbacks
{
    private readonly IAdapterHubClient _hubClient;
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly IOptions<AdapterOptions> _adapterOptions;
    private readonly IAdapterService _adapterService;
    private readonly IPipelineExecutionReporter? _executionReporter;
    private readonly IPipelineRegistryService _pipelineRegistryService;
    private readonly SemaphoreSlim _configurationUpdateLock = new(1, 1);

    /// <summary>
    /// Creates a new instance of <see cref="AdapterExecutionService"/>.
    /// </summary>
    /// <param name="adapterHubClient"></param>
    /// <param name="adapterOptions"></param>
    /// <param name="adapterService"></param>
    /// <param name="adapterHubCallbackService"></param>
    /// <param name="adapterLifetimeManagement"></param>
    /// <param name="pipelineRegistryService"></param>
    /// <param name="executionReporter"></param>
    public AdapterExecutionService(IAdapterHubClient adapterHubClient,
        IOptions<AdapterOptions> adapterOptions, IAdapterService adapterService,
        IAdapterHubCallbackService adapterHubCallbackService,
        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        AdapterLifetimeManagement adapterLifetimeManagement,
        IPipelineRegistryService pipelineRegistryService,
        IPipelineExecutionReporter? executionReporter = null)
    {
        _adapterService = adapterService;
        _hubClient = adapterHubClient;
        _adapterOptions = adapterOptions;
        _pipelineRegistryService = pipelineRegistryService;
        _executionReporter = executionReporter;

        // AdapterLifetimeManagement is used to stop the adapter from an external source
        // it only needs to be created via DI container and is then accessed from the outside
        // which only happens if a service requires it in the constructor. So that's why the unused
        // parameter is not removed.

        adapterHubCallbackService.RegisterCallback(this);
    }

    /// <inheritdoc />
    public Task PreUpdateTenantAsync(string tenantId)
    {
        _logger.Info("PreUpdateTenantAsync for tenant {TenantId}", tenantId);

        // Run on a background thread to avoid deadlocks on the SignalR callback thread.
        _ = Task.Run(async () =>
        {
            try
            {
                var cancellationToken = CancellationToken.None;
                await StopAsync(cancellationToken);

                _logger.Info("Waiting for 5 seconds to reconnect to service...");
                await Task.Delay(5000, cancellationToken);

                _logger.Info("PreUpdateTenantAsync for tenant {TenantId} finished", tenantId);

                await StartAsync(cancellationToken);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error during PreUpdateTenantAsync for tenant {TenantId}", tenantId);
            }
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task AdapterConfigurationUpdatedAsync(string tenantId, AdapterConfigurationDto adapterConfiguration)
    {
        _logger.Info("AdapterConfigurationUpdatedAsync for tenant {TenantId}", tenantId);

        // Run shutdown/startup on a background thread to avoid deadlocks.
        // This method is called on the SignalR callback thread. The MassTransit bus stop
        // during shutdown may wait for in-flight consumers that need the SignalR thread,
        // causing a deadlock if we await directly here.
        _ = Task.Run(() => ApplyConfigurationUpdateAsync(tenantId, adapterConfiguration));

        return Task.CompletedTask;
    }

    private static readonly TimeSpan ConfigurationUpdateLockTimeout = TimeSpan.FromSeconds(90);

    private async Task ApplyConfigurationUpdateAsync(string tenantId, AdapterConfigurationDto adapterConfiguration)
    {
        var totalStopwatch = Stopwatch.StartNew();
        var cancellationToken = CancellationToken.None;

        _logger.Info("Waiting to acquire configuration update lock for tenant {TenantId}", tenantId);
        var lockStopwatch = Stopwatch.StartNew();
        if (!await _configurationUpdateLock.WaitAsync(ConfigurationUpdateLockTimeout, cancellationToken))
        {
            _logger.Error(
                "Timed out after {ElapsedMs}ms waiting for configuration update lock for tenant {TenantId}. " +
                "Another configuration update is likely still in progress.",
                lockStopwatch.ElapsedMilliseconds, tenantId);
            try
            {
                var rtEntityId = GetAdapterRtEntityId();
                await _hubClient.SendDeploymentUpdateResultAsync(rtEntityId,
                    new DeploymentResult
                    {
                        IsSuccess = false,
                        ErrorMessages =
                        [
                            new DeploymentUpdateErrorMessageDto
                            {
                                ErrorCategory = DeploymentErrorCategories.Uncategorized,
                                ErrorMessage =
                                    $"Configuration update lock timeout after {ConfigurationUpdateLockTimeout.TotalSeconds}s"
                            }
                        ]
                    });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to send lock timeout error result for tenant {TenantId}", tenantId);
            }

            return;
        }

        _logger.Info("Acquired configuration update lock for tenant {TenantId} after {ElapsedMs}ms",
            tenantId, lockStopwatch.ElapsedMilliseconds);

        try
        {
            List<DeploymentUpdateErrorMessageDto> deploymentErrorMessages = [];

            _logger.Info("Starting shutdown for configuration update of tenant {TenantId}", tenantId);
            var stepStopwatch = Stopwatch.StartNew();
            await _adapterService.ShutdownAsync(
                new AdapterShutdown { TenantId = tenantId, StopEventHub = false }, cancellationToken);
            _logger.Info("Shutdown completed for tenant {TenantId} in {ElapsedMs}ms",
                tenantId, stepStopwatch.ElapsedMilliseconds);

            _logger.Info("Starting startup for configuration update of tenant {TenantId}", tenantId);
            stepStopwatch.Restart();
            var startupSuccess = await _adapterService.StartupAsync(
                new AdapterStartup
                {
                    TenantId = tenantId,
                    Configuration = adapterConfiguration,
                    StartEventHub = false
                }, deploymentErrorMessages, cancellationToken);
            _logger.Info("Startup completed for tenant {TenantId} in {ElapsedMs}ms, success={Success}",
                tenantId, stepStopwatch.ElapsedMilliseconds, startupSuccess);

            var rtEntityId = GetAdapterRtEntityId();
            await _hubClient.SendDeploymentUpdateResultAsync(rtEntityId,
                new DeploymentResult { IsSuccess = startupSuccess, ErrorMessages = deploymentErrorMessages });

            _logger.Info(
                "Configuration update completed for tenant {TenantId} in {TotalElapsedMs}ms (lock wait: {LockWaitMs}ms)",
                tenantId, totalStopwatch.ElapsedMilliseconds, lockStopwatch.ElapsedMilliseconds);
        }
        catch (Exception e)
        {
            _logger.Error(e,
                "Error during ApplyConfigurationUpdateAsync for tenant {TenantId} after {ElapsedMs}ms",
                tenantId, totalStopwatch.ElapsedMilliseconds);
            try
            {
                var rtEntityId = GetAdapterRtEntityId();
                await _hubClient.SendDeploymentUpdateResultAsync(rtEntityId,
                    new DeploymentResult
                    {
                        IsSuccess = false,
                        ErrorMessages =
                        [
                            new DeploymentUpdateErrorMessageDto
                                { ErrorCategory = DeploymentErrorCategories.Uncategorized, ErrorMessage = e.Message }
                        ]
                    });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to send deployment error result for tenant {TenantId}", tenantId);
            }
        }
        finally
        {
            _configurationUpdateLock.Release();
        }
    }

    /// <summary>
    /// Starts the adapter execution service.
    /// </summary>
    /// <param name="cancellationToken"></param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            async Task ReConnectFunction(bool isReconnect)
            {
                try
                {
                    _logger.Info("Registering at adapter hub");

                    var rtEntityId = GetAdapterRtEntityId();
                    var configuration = await _hubClient.RegisterAdapterAsync(rtEntityId);
                    _logger.Info("Registration successfull");

                    List<DeploymentUpdateErrorMessageDto> deploymentErrorMessages = [];
                    bool success;
                    if (!isReconnect)
                    {
                        var tenantId = _adapterOptions.Value.TenantId;
                        if (string.IsNullOrWhiteSpace(tenantId) || tenantId == null)
                        {
                            return;
                        }

                        // Ensure that the adapter is shutdown before starting it up again
                        // to stop e.g. trigger nodes that are still running
                        _logger.Info("Ensure shutdown adapter before startup.");
                        await _adapterService.ShutdownAsync(new AdapterShutdown { TenantId = tenantId },
                            cancellationToken);
                        _logger.Info("Shutdown adapter before startup done.");

                        _logger.Info("Startup of adapter is executed.");
                        success = await _adapterService.StartupAsync(
                            new AdapterStartup { TenantId = tenantId, Configuration = configuration },
                            deploymentErrorMessages, cancellationToken);
                        _logger.Info("Startup of adapter done.");
                    }
                    else
                    {
                        success = true;

                        // Handle interrupted executions after reconnect
                        await HandleInterruptedExecutionsAsync();
                    }

                    _logger.Info("Sending deployment result to adapter hub");
                    await _hubClient.SendDeploymentUpdateResultAsync(rtEntityId,
                        new DeploymentResult { IsSuccess = success, ErrorMessages = deploymentErrorMessages });
                    _logger.Info("Deployment result sent to adapter hub");
                }
                catch (ObjectDisposedException)
                {
                    _logger.Warn("Hub connection was disposed during reconnect, skipping error report");
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Error during reconnect of adapter");

                    try
                    {
                        var rtEntityId = GetAdapterRtEntityId();
                        await _hubClient.SendDeploymentUpdateResultAsync(rtEntityId,
                            new DeploymentResult
                            {
                                IsSuccess = false,
                                ErrorMessages =
                                [
                                    new DeploymentUpdateErrorMessageDto
                                    {
                                        ErrorCategory = DeploymentErrorCategories.Uncategorized,
                                        ErrorMessage = e.Message
                                    }
                                ]
                            });
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn(ex, "Failed to send deployment error result during reconnect");
                    }
                }
            }

            await StartCommunicationAsync(cancellationToken, ReConnectFunction);
        }
        catch (Exception e)
        {
            _logger.Error(e, "Error during initialization of adapter execution service");

            var rtEntityId = GetAdapterRtEntityId();
            await _hubClient.SendDeploymentUpdateResultAsync(rtEntityId,
                new DeploymentResult
                {
                    IsSuccess = false,
                    ErrorMessages =
                    [
                        new DeploymentUpdateErrorMessageDto
                            { ErrorCategory = DeploymentErrorCategories.Uncategorized, ErrorMessage = e.Message }
                    ]
                });
        }
    }

    /// <summary>
    /// Stops the adapter execution service.
    /// </summary>
    /// <param name="cancellationToken"></param>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = _adapterOptions.Value.TenantId;
            if (string.IsNullOrWhiteSpace(tenantId) || tenantId == null)
            {
                return;
            }

            await _adapterService.ShutdownAsync(new AdapterShutdown { TenantId = tenantId }, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_adapterOptions.Value.AdapterRtId))
            {
                var rtEntityId = GetAdapterRtEntityId();
                await _hubClient.UnRegisterAdapterAsync(rtEntityId);
            }

            await _hubClient.StopAsync();
        }
        catch (Exception e)
        {
            _logger.Error(e, "Error during deinitialization of adapter execution service");
        }
    }

    private RtEntityId GetAdapterRtEntityId()
    {
        if (!string.IsNullOrWhiteSpace(_adapterOptions.Value.AdapterRtId) &&
            !string.IsNullOrWhiteSpace(_adapterOptions.Value.AdapterCkTypeId))
        {
            var adapterRtId = OctoObjectId.Parse(_adapterOptions.Value.AdapterRtId!);
            var adapterCkId = new RtCkId<CkTypeId>(_adapterOptions.Value.AdapterCkTypeId!);
            var rtEntityId = new RtEntityId(adapterCkId, adapterRtId);
            return rtEntityId;
        }

        throw AdapterException.ConfigurationErrorAdapterRtIdAdapterCkTypeIdNotSet();
    }

    private async Task StartCommunicationAsync(CancellationToken stoppingToken,
        Func<bool, Task> onReconnectFunc)
    {
        _logger.Info("Starting adapter...");
        _logger.Info("Connecting to adapter hub at {CommunicationControllerServicesUri}",
            _adapterOptions.Value.CommunicationControllerServicesUri);
        _logger.Info("TenantId {TenantId}, AdapterRtId {AdapterRtId}",
            _adapterOptions.Value.TenantId, _adapterOptions.Value.AdapterRtId);

        if (_adapterOptions.Value.AdapterRtId == null)
        {
            _logger.Error("AdapterRtId is null");
            return;
        }

        await _hubClient.StartAsync(onReconnectFunc, stoppingToken);
        _logger.Info("Connected to adapter hub");

        if (stoppingToken.IsCancellationRequested)
        {
            await _hubClient.StopAsync();
            return;
        }

        _logger.Info("Enabling automatic reconnect");
        _hubClient.EnableReconnect(onReconnectFunc);
    }

    /// <summary>
    /// Handles interrupted executions after adapter reconnects.
    /// Queries the server for executions that were marked as interrupted and reports their final status.
    /// </summary>
    private async Task HandleInterruptedExecutionsAsync()
    {
        if (_executionReporter == null)
        {
            return;
        }

        try
        {
            _logger.Info("Checking for interrupted executions...");
            var interruptedIds = await _executionReporter.GetInterruptedExecutionIdsAsync();

            if (interruptedIds.Count == 0)
            {
                _logger.Info("No interrupted executions found");
                return;
            }

            _logger.Info("Found {Count} interrupted executions to report", interruptedIds.Count);

            var tenantId = _adapterOptions.Value.TenantId;
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return;
            }

            foreach (var executionIdString in interruptedIds)
            {
                try
                {
                    if (!Guid.TryParse(executionIdString, out var executionId))
                    {
                        _logger.Warn("Invalid execution ID format: {ExecutionId}", executionIdString);
                        continue;
                    }

                    // Try to find the execution in local pipeline registrations
                    var (status, startTime) = GetLocalExecutionStatus(tenantId!, executionId);

                    var completedAt = DateTime.UtcNow;
                    var durationMs = startTime.HasValue
                        ? (int)(completedAt - startTime.Value).TotalMilliseconds
                        : 0;

                    await _executionReporter.ReportInterruptedExecutionResultAsync(
                        executionId,
                        status,
                        completedAt,
                        durationMs,
                        status == PipelineExecutionStatus.Failed ? "Execution failed during disconnect" : null);

                    _logger.Info("Reported final status {Status} for interrupted execution {ExecutionId}",
                        status, executionId);
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to report interrupted execution {ExecutionId}", executionIdString);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Warn(ex, "Error handling interrupted executions");
        }
    }

    /// <summary>
    /// Gets the local execution status for an execution ID.
    /// </summary>
    private (PipelineExecutionStatus status, DateTime? startTime) GetLocalExecutionStatus(string tenantId, Guid executionId)
    {
        // Check all registered pipelines for this execution
        foreach (var pipelineRtEntityId in _pipelineRegistryService.GetRegisteredPipelines(tenantId))
        {
            if (_pipelineRegistryService.TryGetPipelineRegistration(tenantId, pipelineRtEntityId,
                    out var registration) && registration != null)
            {
                var status = registration.GetPipelineExecutionStatus(executionId);
                var startTime = registration.GetExecutionStartTime(executionId);

                // If we found a non-failed status, or if we found the execution at all
                if (status != PipelineExecutionStatus.Failed || startTime.HasValue)
                {
                    return (status, startTime);
                }
            }
        }

        // Execution not found locally - report as completed (optimistic)
        // This handles the case where the execution completed and was removed from memory
        return (PipelineExecutionStatus.Completed, null);
    }
}