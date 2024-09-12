using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.Services;
using Meshmakers.Octo.Sdk.SimulationNodes.Generators;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.SimulationNodes.Nodes.Loads;

/// <summary>
/// Configuration for the simulation node
/// </summary>
[NodeName("Simulation", 1)]
public class SimulationNodeConfiguration : NodeConfiguration
{
    /// <summary>
    /// List of transformations to apply to the signal
    /// </summary>
    public ICollection<SimulationPropertyConfiguration>? Simulations { get; set; }
}

/// <summary>
/// Configuration for a single simulation property
/// </summary>
public class SimulationPropertyConfiguration
{
    /// <summary>
    /// Target path to set the value to
    /// </summary>
    public string TargetPath { get; set; } = null!;

    /// <summary>
    /// Kind of simulator to use
    /// </summary>
    public string SimulatorKey { get; set; } = "Increment";
    
    /// <summary>
    /// Configuration for the simulator
    /// </summary>
    public string Configuration { get; set; } = "{}";
}

/// <summary>
/// Generates a value for a target path using a simulator
/// </summary>
/// <param name="next">Next node in the pipeline</param>
/// <param name="etlContext">The ETL context</param>
[NodeConfiguration(typeof(SimulationNodeConfiguration))]
public class SimulationNode(NodeDelegate next, IEtlContext etlContext) : IPipelineNode
{
    /// <inheritdoc />
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