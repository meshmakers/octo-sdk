using Meshmakers.Common.Shared;
using Meshmakers.Octo.Sdk.Common.Adapters;
using Meshmakers.Octo.Sdk.Common.Services;
using NLog;
using Sdk.Plug.Simulation.Configuration;

namespace Sdk.Plug.Simulation;

public class SimulationAdapterService(
    IPollingService pollingService,
    IPipelineExecutionService pipelineExecutionService)
    : IAdapterService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public async Task StartupAsync(AdapterStartup adapterStartup, CancellationToken stoppingToken)
    {
        try
        {
            Logger.Info("SimulationPlugService started");

            if (adapterStartup.Configuration.Configuration == null)
            {
                throw new Exception("No configuration received");
            }

            if (adapterStartup.Configuration.DataPipelineConfigurations == null)
            {
                throw new Exception("No data pipeline configuration received");
            }

            var simulationConfiguration = adapterStartup.Configuration.Configuration.Deserialize<SimulationConfiguration>();

            foreach (var dataPipelineConfiguration in adapterStartup.Configuration.DataPipelineConfigurations)
            {
                await pipelineExecutionService.RegisterPipeline(adapterStartup.TenantId, dataPipelineConfiguration);
            }

            pollingService.AddCallback(simulationConfiguration.Interval, async () =>
            {
                await pipelineExecutionService.ExecuteAllPipelinesAsync(new ExecutePipelineOptions(DateTime.UtcNow));
            });
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