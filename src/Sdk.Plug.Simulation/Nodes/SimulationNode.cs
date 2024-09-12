using System.Text.Json.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Plug.Simulation.Generators;

namespace Sdk.Plug.Simulation.Nodes;

[NodeName("Simulation", 1)]
internal class SimulationNodeConfiguration : NodeConfiguration
{
    /// <summary>
    /// List of transformations to apply to the signal
    /// </summary>
    public ICollection<SimulationPropertyConfiguration>? Simulations { get; set; }
}

internal class SimulationPropertyConfiguration
{
    public string TargetPath { get; set; } = null!;

    public string SimulatorKey { get; set; } = "Increment";
    
    public string Configuration { get; set; } = "{}";
}


[NodeConfiguration(typeof(SimulationNodeConfiguration))]
internal class SimulationNode(NodeDelegate next, IEtlContext etlContext) : IPipelineNode
{
    public Task ProcessObjectAsync(IDataContext dataContext)
    {
        var c = dataContext.GetNodeConfiguration<SimulationNodeConfiguration>();

        if (c.Simulations != null)
        {
            foreach (var simulation in c.Simulations)
            {
                var generator = dataContext.GlobalServiceProvider.GetKeyedService<IValueGenerator>(simulation.SimulatorKey);
                if (generator == null)
                {
                    throw new PipelineNodeExecutionException("No generator found for key: " + simulation.SimulatorKey);
                }
                var config = JObject.Parse(simulation.Configuration);
                var value = generator.Generate(etlContext, config);
                if (value == null)
                {
                    dataContext.SetCurrentValueByPath<object>(simulation.TargetPath, null);
                }
                else
                {
                    dataContext.SetCurrentValueByPath(simulation.TargetPath, value);
                }
            }
        }

        return next(dataContext);
    }
}