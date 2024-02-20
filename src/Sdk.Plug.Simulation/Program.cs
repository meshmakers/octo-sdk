using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.Plugs;
using Microsoft.Extensions.DependencyInjection;
using Sdk.Plug.Simulation;

var plugBuilder = new PlugBuilder();

plugBuilder.Run(args, (_, services) =>
{
    services.AddDataPipeline();
    services.AddTransient<IExtractPipelineNode, SimulationNode>();
    services.AddSingleton<IPlugService, SimulationPlugService>();
});