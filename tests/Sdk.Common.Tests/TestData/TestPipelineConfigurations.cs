using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;
using Sdk.Common.Tests.TestData.Dto;

namespace Sdk.Common.Tests.TestData;

internal static class TestPipelineConfigurations
{
    public static PipelineConfigurationRoot Test1 => new()
    {
        Transformations = new List<NodeConfiguration>
        {
            new TestDataExtractNodeConfiguration
            {
                Description = "Test data extract node",
                Data = Generator.GenerateOrder()
            },
            new SelectByPathNodeConfiguration
            {
                Description = "Transform object node",
                Transformations = new List<PathPropertyConfigurationNode>
                {
                    new()
                    {
                        SourcePath = "$.InvoiceNumber",
                        TargetPropertyName = "InvoiceNumber",
                        Transformations = new List<NodeConfiguration>
                        {
                            new LinearScalerNodeConfiguration
                            {
                                ScaleInputMin = 0,
                                ScaleInputMax = 100,
                                ScaleOutputMin = 0,
                                ScaleOutputMax = 1000
                            },
                            new ConvertDataTypeNodeConfiguration
                            {
                                ValueType = AttributeValueTypesDto.Double
                            }
                            
                        }
                    },
                    new()
                    {
                        SourcePath = "$.Items",
                        TargetPropertyName = "OrderItems",
                        Transformations = new List<NodeConfiguration>
                        {
                            new SelectByPathNodeConfiguration
                            {
                                Transformations = new List<PathPropertyConfigurationNode>
                                {
                                    new()
                                    {
                                        SourcePath = "$.TransactionId",
                                        TargetPropertyName = "TransactionId",
                                        Transformations = new List<NodeConfiguration>
                                        {
                                            new ConvertDataTypeNodeConfiguration
                                            {
                                                ValueType = AttributeValueTypesDto.String
                                            }
                                        }
                                    },
                                    new()
                                    {
                                        SourcePath = "$.Quantity",
                                        TargetPropertyName = "Quantity",
                                        Transformations = new List<NodeConfiguration>
                                        {
                                            new ConvertDataTypeNodeConfiguration
                                            {
                                                ValueType = AttributeValueTypesDto.Int64
                                            }
                                        }
                                    },
                                }
                            }
                        }
                    },
                }
            },
            new ProjectNodeConfiguration
            {
                Fields = new List<FieldConfiguration>
                {
                    new(){ Path = "$.Items"},
                    new(){ Path = "$.Customer"},
                    new(){ Path = "$.InvoiceAddress"},
                    new(){ Path = "$.ShippingAddress"}
                }
            }
        }
    };
}