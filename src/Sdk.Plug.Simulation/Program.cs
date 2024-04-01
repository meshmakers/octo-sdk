using Meshmakers.Octo.Sdk.Common.Adapters;
using Meshmakers.Octo.Sdk.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Sdk.Plug.Simulation;
using Sdk.Plug.Simulation.Nodes;

var plugBuilder = new AdapterBuilder();

plugBuilder.Run(args, (_, services) =>
{
    services.AddDataPipeline()
        .RegisterNode<SimulationNode>()
        .RegisterEtlContext<IAdapterEtlContext>();
    services.AddTransient<IPollingService, PollingService>();
    services.AddSingleton<IAdapterService, SimulationAdapterService>();
});