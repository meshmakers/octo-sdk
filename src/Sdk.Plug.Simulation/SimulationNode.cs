using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Sdk.Plug.Simulation.Configuration;

namespace Sdk.Plug.Simulation;

internal class SimulationNodeConfiguration : ExtractNodeConfiguration
{
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// List of transformations to apply to the signal
    /// </summary>
    public ICollection<SimulationPropertyConfiguration>? Simulations { get; set; }
}

internal class SimulationPropertyConfiguration
{
    public string PropertyName { get; set; } = null!;
    
    public SimulationTypes SimulationTypes { get; set; } = SimulationTypes.Sinus;
    
    public double? Parameter1 { get; set; }
}


[Node("Simulation", 1, typeof(SimulationNodeConfiguration))]
internal class SimulationNode : IExtractPipelineNode
{
    public Task ProcessObjectAsync(IExtractDataContext dataContext)
    {
        var c = dataContext.GetNodeConfiguration<SimulationNodeConfiguration>();

        if (c.Simulations != null)
        {
            foreach (var simulation in c.Simulations)
            {
                switch (simulation.SimulationTypes)
                {
                    case SimulationTypes.Sinus:
                        // Apply sinus simulation
                        break;
                    case SimulationTypes.Constant:
                        break;
                    case SimulationTypes.Triangle:
                        break;
                }
            }
        }

        return Task.CompletedTask;
    }
}