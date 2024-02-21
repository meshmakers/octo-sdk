using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;
using Sdk.Common.Tests.TestData.Dto;

namespace Sdk.Common.Tests.TestData;

internal static class TestPipelineConfigurations
{
    public static PipelineConfigurationRoot Test1 => new()
    {
        Extracts = new List<ExtractNodeConfiguration>
        {
            new TestDataExtractNodeConfiguration
            {
                Description = "Test data extract node",
                Data = Generator.GenerateOrder()
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
                        SourcePath = "$.InvoiceNumber",
                        TargetPropertyName = "InvoiceNumber",
                        Transforms = new List<TransformNodeConfiguration>
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
                        Transforms = new List<TransformNodeConfiguration>
                        {
                            new ByPathNodeConfiguration
                            {
                                Transformations = new List<PathPropertyConfigurationNode>
                                {
                                    new()
                                    {
                                        SourcePath = "$.TransactionId",
                                        TargetPropertyName = "TransactionId",
                                        Transforms = new List<TransformNodeConfiguration>
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
                                        Transforms = new List<TransformNodeConfiguration>
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
            }
        }
    };
}