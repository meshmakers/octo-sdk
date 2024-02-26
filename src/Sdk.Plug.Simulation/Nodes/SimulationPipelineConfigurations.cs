using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Loads;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;
using Sdk.Plug.Simulation.Configuration;

namespace Sdk.Plug.Simulation.Nodes
{
    internal static class SimulationPipelineConfigurations
    {
        public static PipelineConfigurationRoot Test2 => new()
        {
            Transformations = new List<NodeConfiguration>
            {
                new SimulationNodeConfiguration
                {
                    Description = "Simulates data",
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
                },
                new SplitterNodeConfiguration()
                {
                    Transformations = new List<NodeConfiguration>()
                    {
                        new SequenceNodeConfiguration
                        {
                            Transformations = new List<NodeConfiguration>
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
                        new SequenceNodeConfiguration
                        {
                            Transformations = new List<NodeConfiguration>
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
                        new SequenceNodeConfiguration
                        {
                            Transformations = new List<NodeConfiguration>
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
                    }
                }
            }
        };


        public static PipelineConfigurationRoot Test1 => new()
        {
            Transformations = new List<NodeConfiguration>
            {
                new SimulationNodeConfiguration
                {
                    Description = "Simulates data",
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
                },
                new SelectByPathNodeConfiguration
                {
                    Description = "Transform object node",
                    Transformations = new List<PathPropertyConfigurationNode>
                    {
                        new()
                        {
                            SourcePath = "$.Sinus",
                            TargetPropertyName = "Sinus5",
                            Transformations = new List<NodeConfiguration>
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
                            Transformations = new List<NodeConfiguration>
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
                },
                new ProjectNodeConfiguration
                {
                    Fields = new List<FieldConfiguration>
                    {
                        new()
                        {
                            Path = "$.Sinus",
                        },
                        new()
                        {
                            Path = "$.Constant_6",
                        }
                    }
                },
                new DistributionEventHubNodeConfiguration
                {
                    Description = "Load to event hub",
                }
            }
        };
    }
}