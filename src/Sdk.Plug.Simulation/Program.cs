using Meshmakers.Octo.Sdk.Common.Adapters;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Sdk.Plug.Simulation;
using Sdk.Plug.Simulation.Nodes;

var plugBuilder = new AdapterBuilder();

plugBuilder.Run(args, (_, services) =>
{
    services.AddDataPipeline();
    services.AddTransient<IPollingService, PollingService>();
    services.AddTransient<IExtractPipelineNode, SimulationNode>();
    services.AddSingleton<IAdapterService, SimulationAdapterService>();
});