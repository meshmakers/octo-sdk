using Meshmakers.Common.Shared;
using Meshmakers.Octo.Common.DistributionEventHub.Services;
using Meshmakers.Octo.Sdk.Common.Adapters;
using Meshmakers.Octo.Sdk.Common.Services;
using NLog;
using Sdk.Plug.Simulation.Configuration;

namespace Sdk.Plug.Simulation;

public class SimulationAdapterService(
    IPollingService pollingService,
    IPipelineExecutionService pipelineExecutionService, IEventHubControl eventHubControl)
    : IAdapterService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public async Task StartupAsync(AdapterStartup adapterStartup, CancellationToken stoppingToken)
    {
        try
        {
            Logger.Info("SimulationPlugService started");

            if (adapterStartup.Configuration.AdapterConfiguration == null)
            {
                throw new Exception("No configuration received");
            }

            if (adapterStartup.Configuration.Pipelines == null)
            {
                throw new Exception("No data pipeline configuration received");
            }

            var simulationConfiguration = adapterStartup.Configuration.AdapterConfiguration.Deserialize<SimulationConfiguration>();

            foreach (var dataPipelineConfiguration in adapterStartup.Configuration.Pipelines)
            {
                await pipelineExecutionService.RegisterPipeline(adapterStartup.TenantId, dataPipelineConfiguration);
            }

            pollingService.AddCallback(simulationConfiguration.Interval, async () =>
            {
                await pipelineExecutionService.ExecuteAllPipelinesAsync(new ExecutePipelineOptions(DateTime.UtcNow, adapterStartup.SendDebugInfoFunc));
            });
            await eventHubControl.StartAsync(stoppingToken);
            pollingService.Start();
        }
        catch (Exception e)
        {
            Logger.Error(e, "Error while startup");
            throw;
        }
    }

    public Task ShutdownAsync(AdapterShutdown adapterShutdown, CancellationToken stoppingToken)
    {
        try
        {
            pollingService.Stop();
            pollingService.ClearCallbacks();
            
            pipelineExecutionService.UnregisterAllPipelines(adapterShutdown.TenantId);
            Logger.Info("SimulationPlugService stopped");
            return Task.CompletedTask;
        }
        catch (Exception e)
        {
            Logger.Error(e, "Error while shutdown");
            throw;
        }
    }
}