using Meshmakers.Common.Shared;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.Adapters;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration.Serializer;
using Meshmakers.Octo.Sdk.Common.Services;
using NLog;
using Sdk.Plug.Simulation.Configuration;
using Sdk.Plug.Simulation.Nodes;

namespace Sdk.Plug.Simulation;

public class SimulationAdapterService : IAdapterService
{
    private readonly IPollingService _pollingService;
    private readonly IEtlDataOrchestrator _etlDataOrchestrator;
    private readonly IJsonPipelineConfigurationSerializer _jsonPipelineConfigurationSerializer;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public SimulationAdapterService(IPollingService pollingService, IEtlDataOrchestrator etlDataOrchestrator,
        IJsonPipelineConfigurationSerializer jsonPipelineConfigurationSerializer)
    {
        _pollingService = pollingService;
        _etlDataOrchestrator = etlDataOrchestrator;
        _jsonPipelineConfigurationSerializer = jsonPipelineConfigurationSerializer;
    }

    public async Task StartupAsync(AdapterStartup adapterStartup, CancellationToken stoppingToken)
    {
        try
        {
            Logger.Info("SimulationPlugService started");

            var r = await _jsonPipelineConfigurationSerializer.SerializeAsync(SimulationPipelineConfigurations.Test1);
            // var x = await _jsonPipelineConfigurationSerializer.DeserializeAsync(r);

            if (adapterStartup.Configuration.Configuration == null)
            {
                throw new Exception("No configuration received");
            }

            if (adapterStartup.Configuration.DataPipelineConfigurations == null)
            {
                throw new Exception("No data pipeline configuration received");
            }

            var simulationConfiguration = adapterStartup.Configuration.Configuration.Deserialize<SimulationConfiguration>();

            List<Tuple<DataPipelineConfigurationDto, PipelineConfigurationRoot, Dictionary<string, object?>>> lst = new();
            foreach (var dataPipelineConfiguration in adapterStartup.Configuration.DataPipelineConfigurations)
            {
                //var configurationRoot = await _jsonPipelineConfigurationSerializer.DeserializeAsync(dataPipelineConfiguration.DataPipelineConfiguration);
                var configurationRoot = SimulationPipelineConfigurations.Test1;
                lst.Add(new Tuple<DataPipelineConfigurationDto, PipelineConfigurationRoot, Dictionary<string, object?>>(
                    dataPipelineConfiguration, configurationRoot, new Dictionary<string, object?>()));
            }

            _pollingService.AddCallback(simulationConfiguration.Interval, async () =>
            {
                foreach (var tuple in lst)
                {
                    try
                    {
                        Logger.Info("Execute pipeline {Id}: {Name}", tuple.Item1.DataPipelineRtId, tuple.Item1.Name);
                        await _etlDataOrchestrator.ExecutePipelineAsync<IAdapterEtlContext>(tuple.Item2,
                            new AdapterEtlContext(adapterStartup.TenantId, tuple.Item1.DataPipelineRtId, tuple.Item3));
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "Error while executing pipeline {Id}: {Name}", tuple.Item1.DataPipelineRtId, tuple.Item1.Name);
                    }
                }
            });
            _pollingService.Start();
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