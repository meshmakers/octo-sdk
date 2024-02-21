using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.Plugs;
using Meshmakers.Octo.Sdk.Common.Services;
using NLog;
using Sdk.Plug.Simulation.Nodes;

namespace Sdk.Plug.Simulation;

public class SimulationPlugService : IPlugService
{
    private readonly IPollingService _pollingService;
    private readonly IServiceProvider _serviceProvider;
    private readonly INodeLookupService _nodeLookupService;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public SimulationPlugService(IPollingService pollingService, IServiceProvider serviceProvider, INodeLookupService nodeLookupService)
    {
        _pollingService = pollingService;
        _serviceProvider = serviceProvider;
        _nodeLookupService = nodeLookupService;
    }

    public Task StartupAsync(PlugStartup plugStartup, CancellationToken stoppingToken)
    {
        try
        {
            Logger.Info("SimulationPlugService started");
            //
            // foreach (var configurationServerConfiguration in plugStartup.Configuration.ServerConfigurations)
            // {
            //     foreach (var groupConfigurationDto in configurationServerConfiguration.Groups)
            //     {
            //         groupConfigurationDto.
            //     }
            // }

            var c = SimulationPipelineConfigurations.Test1;
            
            _pollingService.AddCallback(new TimeSpan(0, 0, 0, 10), async () =>
            {
                var etlDataOrchestrator = new EtlDataOrchestrator(_serviceProvider, c, _nodeLookupService);
                await etlDataOrchestrator.ExecutePipelineAsync();
            });

            return Task.CompletedTask;
        }
        catch (Exception e)
        {
            Logger.Error(e, "Error while startup");
            throw;
        }
    }

    public Task ShutdownAsync(CancellationToken stoppingToken)
    {
        try
        {
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