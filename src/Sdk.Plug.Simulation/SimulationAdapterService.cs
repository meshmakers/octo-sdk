using Meshmakers.Octo.Common.DistributionEventHub.Services;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.Adapters;
using Meshmakers.Octo.Sdk.Common.Services;
using NLog;

namespace Sdk.Plug.Simulation;

public class SimulationAdapterService(
    IPipelineRegistryService pipelineRegistryService,
    IEventHubControl eventHubControl)
    : IAdapterService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public async Task<bool> StartupAsync(AdapterStartup adapterStartup,
        List<DeploymentUpdateErrorMessageDto> errorMessages,
        CancellationToken stoppingToken)
    {
        try
        {
            Logger.Info("SimulationPlugService started");

            if (adapterStartup.Configuration.AdapterConfiguration == null)
            {
                throw new Exception("No configuration received");
            }

            var success = await pipelineRegistryService.RegisterPipelinesAsync(adapterStartup.TenantId,
                adapterStartup.Configuration.Pipelines, errorMessages);
            await pipelineRegistryService.StartTriggerPipelineNodesAsync(adapterStartup.TenantId);

            await eventHubControl.StartAsync(stoppingToken);

            return success;
        }
        catch (Exception e)
        {
            Logger.Error(e, "Error while startup");
            throw;
        }
    }

    public async Task ShutdownAsync(AdapterShutdown adapterShutdown, CancellationToken stoppingToken)
    {
        try
        {
            Logger.Info("SimulationPlugService stopping");
            await pipelineRegistryService.StopTriggerPipelineNodesAsync(adapterShutdown.TenantId);

            pipelineRegistryService.UnregisterAllPipelines(adapterShutdown.TenantId);

            await eventHubControl.StopAsync(stoppingToken);

            Logger.Info("SimulationPlugService stopped");
        }
        catch (Exception e)
        {
            Logger.Error(e, "Error while shutdown");
            throw;
        }
    }
}