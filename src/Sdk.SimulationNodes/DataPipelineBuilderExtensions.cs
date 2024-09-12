using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration.DependencyInjection;
using Meshmakers.Octo.Sdk.SimulationNodes.Generators;
using Meshmakers.Octo.Sdk.SimulationNodes.Nodes.Loads;
using Microsoft.Extensions.DependencyInjection;

namespace Meshmakers.Octo.Sdk.SimulationNodes;

/// <summary>
/// Extensions for simulation pipeline nodes
/// </summary>
public static class DataPipelineBuilderExtensions
{
    /// <summary>
    /// Adds simulation nodes for pipeline
    /// </summary>
    /// <param name="pipelineBuilder"></param>
    /// <returns></returns>
    public static IDataPipelineBuilder AddSimulationNodes(this IDataPipelineBuilder pipelineBuilder)
    {
        pipelineBuilder.RegisterNodeConfiguration<SimulationNodeConfiguration>();
        pipelineBuilder.RegisterNode<SimulationNode>();

        pipelineBuilder.Services.AddKeyedTransient<IValueGenerator, CityGenerator>("Address.City");
        pipelineBuilder.Services.AddKeyedTransient<IValueGenerator, StreetAddressGenerator>("Address.StreetAddress");
        pipelineBuilder.Services.AddKeyedTransient<IValueGenerator, StreetNameGenerator>("Address.StreetName");
        pipelineBuilder.Services.AddKeyedTransient<IValueGenerator, BuildingNumberGenerator>("Address.BuildingNumber");
    
        pipelineBuilder.Services.AddKeyedTransient<IValueGenerator, FirstNameGenerator>("Person.FirstName");
        pipelineBuilder.Services.AddKeyedTransient<IValueGenerator, LastNameGenerator>("Person.LastName");
    
        pipelineBuilder.Services.AddKeyedTransient<IValueGenerator, SinusGenerator>("Math.Sinus");
        pipelineBuilder.Services.AddKeyedTransient<IValueGenerator, TriangleGenerator>("Math.Triangle");
        pipelineBuilder.Services.AddKeyedTransient<IValueGenerator, ConstantGenerator>("Math.Constant");
        
        return pipelineBuilder;
    }
}