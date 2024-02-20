using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;
using Sdk.Plug.Simulation.Configuration;

namespace Sdk.Plug.Simulation
{
    internal static class SimulationPipelineConfigurations
    {
        public static PipelineConfigurationRoot Test1 => new()
        {
            Extracts = new List<ExtractNodeConfiguration>
            {
                new SimulationNodeConfiguration
                {
                    Description = "Simulates data",
                    Interval = new TimeSpan(0, 0, 0, 10),
                    Simulations = new List<SimulationPropertyConfiguration>
                    {
                         new()
                         {
                             PropertyName = "Sinus",
                             SimulationTypes = SimulationTypes.Sinus,
                         },
                         new()
                         {
                             PropertyName = "Constant_6",
                             SimulationTypes = SimulationTypes.Constant,
                             Parameter1 = 6
                         },
                         new()
                         {
                             PropertyName = "Triangle",
                             SimulationTypes = SimulationTypes.Triangle
                         } 
                    }
                }
            },
            Transformations = new List<TransformNodeConfiguration>
            {
                new ByPathNodeConfiguration
                {
                    Description = "Transform object node",
                    Transformations = new List<PathPropertyConfigurationNode>
                    {
                        new()
                        {
                            SourcePath = "$.Sinus",
                            TargetPropertyName = "Sinus5",
                            Transforms = new List<TransformNodeConfiguration>
                            {
                                new LinearScalerNodeConfiguration
                                {
                                    ScaleInputMin = 0,
                                    ScaleInputMax = 1,
                                    ScaleOutputMin = 0,
                                    ScaleOutputMax = 1000
                                }
                            }
                        },
                        new()
                        {
                            SourcePath = "$.Constant_6",
                            TargetPropertyName = "Constant",
                            Transforms = new List<TransformNodeConfiguration>
                            {
                                new LinearScalerNodeConfiguration
                                {
                                    ScaleInputMin = 0,
                                    ScaleInputMax = 10,
                                    ScaleOutputMin = 0,
                                    ScaleOutputMax = 1000
                                }
                            }
                        },
                    }
                }
            }
        };
    }
}