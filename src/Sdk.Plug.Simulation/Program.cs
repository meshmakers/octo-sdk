using Meshmakers.Octo.Sdk.Common.Adapters;
using Meshmakers.Octo.Sdk.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Sdk.Plug.Simulation;
using Sdk.Plug.Simulation.Nodes;

var plugBuilder = new AdapterBuilder();


var timer = new Timer(_ => plugBuilder.Stop(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));


plugBuilder.Run(args, (_, services) =>
{
    services.AddDataPipeline()
        .RegisterNode<SimulationNode>()
        .RegisterNode<WriteJsonNode>()
        .RegisterEtlContext<IAdapterEtlContext>();
    services.AddTransient<IPollingService, PollingService>();
    services.AddSingleton<IAdapterService, SimulationAdapterService>();
});