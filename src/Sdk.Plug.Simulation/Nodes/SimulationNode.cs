using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Plug.Simulation.Configuration;

namespace Sdk.Plug.Simulation.Nodes;

internal class SimulationNodeConfiguration : ExtractNodeConfiguration
{
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
    public double? Parameter2 { get; set; }
    public double? Parameter3 { get; set; }
}


[Node("Simulation", 1, typeof(SimulationNodeConfiguration))]
internal class SimulationNode : IExtractPipelineNode
{
    public Task ProcessObjectAsync(IExtractDataContext dataContext)
    {
        var c = dataContext.GetNodeConfiguration<SimulationNodeConfiguration>();
        var etlContext = dataContext.PipelineServiceProvider.GetRequiredService<IEtlContext>();

        if (c.Simulations != null)
        {
            var jObject = new JObject();
            foreach (var simulation in c.Simulations)
            {
                switch (simulation.SimulationTypes)
                {
                    case SimulationTypes.Sinus:
                        CreateSinus(simulation, etlContext, jObject);
                        break;
                    case SimulationTypes.Constant:
                        jObject[simulation.PropertyName] = simulation.Parameter1 ?? 0;
                        break;
                    case SimulationTypes.Triangle:
                        CreateTriangle(simulation, etlContext, jObject);
                        break;
                }
            }
            dataContext.Source = jObject;
        }

        return Task.CompletedTask;
    }

    private static void CreateTriangle(SimulationPropertyConfiguration simulation, IEtlContext etlContext, JObject jObject)
    {
        var amplitude = simulation.Parameter1 ?? 1;
        var period = TimeSpan.FromSeconds(simulation.Parameter1 ?? 10);
        string name = $"{simulation.PropertyName}_triangle";

        double slope = (4 * amplitude) / period.TotalSeconds; 

        etlContext.Properties.TryAdd(name, DateTime.Now);
        if (etlContext.Properties.TryGetValue(name, out var o) && o is DateTime startTime)
        {
            double elapsed = (DateTime.Now - startTime).TotalSeconds % period.TotalSeconds;
            double value = elapsed < period.TotalSeconds / 2
                ? slope * elapsed - amplitude
                : -slope * elapsed + 3 * amplitude;
            jObject[simulation.PropertyName] = value;
        }
    }

    private static void CreateSinus(SimulationPropertyConfiguration simulation, IEtlContext etlContext, JObject jObject)
    {
        // Apply sinus simulation
        var amplitude = simulation.Parameter1 ?? 1;
        var frequency = simulation.Parameter2 ?? 1;
        string name = $"{simulation.PropertyName}_sinus";

        etlContext.Properties.TryAdd(name, DateTime.Now);
        if (etlContext.Properties.TryGetValue(name, out var o) && o is DateTime startTime)
        {
            double value = amplitude * Math.Sin(2 * Math.PI * frequency * (DateTime.Now - startTime).TotalSeconds);
            jObject[simulation.PropertyName] = value;
        }
    }
}