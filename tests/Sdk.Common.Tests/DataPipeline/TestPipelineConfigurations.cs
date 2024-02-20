using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.DataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.DataPipeline.Nodes.Transforms;
using Sdk.Common.Tests.Dto;

namespace Sdk.Common.Tests.DataPipeline;

internal static class TestPipelineConfigurations
{
    public static PipelineConfigurationRoot Test1 => new()
    {
        Extracts = new List<ExtractConfigurationNode>
        {
            new TestDataExtractConfigurationNode
            {
                Description = "Test data extract node",
                Data = Generator.GenerateOrder()
            }
        },
        Transforms = new List<TransformConfigurationNode>
        {
            new TransformByPathConfigurationNode
            {
                Description = "Transform object node",
                Transforms = new List<TransformPathPropertyConfigurationNode>
                {
                    new()
                    {
                        SourcePath = "$.InvoiceNumber",
                        TargetPropertyName = "InvoiceNumber",
                        ValueType = AttributeValueTypesDto.Int,
                        Transforms = new List<TransformConfigurationNode>
                        {
                            new LinearScalerConfigurationNode
                            {
                                ScaleInputMin = 0,
                                ScaleInputMax = 100,
                                ScaleOutputMin = 0,
                                ScaleOutputMax = 1000
                            }
                        }
                    },
                    new()
                    {
                        SourcePath = "$.Items",
                        TargetPropertyName = "OrderItems",
                        ValueType = AttributeValueTypesDto.RecordArray,
                        Transforms = new List<TransformConfigurationNode>
                        {
                            new TransformByPathConfigurationNode
                            {
                                Transforms = new List<TransformPathPropertyConfigurationNode>
                                {
                                    new()
                                    {
                                        SourcePath = "$.TransactionId",
                                        TargetPropertyName = "TransactionId",
                                        ValueType = AttributeValueTypesDto.String
                                    },
                                    new()
                                    {
                                        SourcePath = "$.Quantity",
                                        TargetPropertyName = "Quantity",
                                        ValueType = AttributeValueTypesDto.Int
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