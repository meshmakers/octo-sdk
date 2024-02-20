using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;
using Sdk.Common.Tests.Dto;

namespace Sdk.Common.Tests.DataPipeline;

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
                        ValueType = AttributeValueTypesDto.Int,
                        Transforms = new List<TransformNodeConfiguration>
                        {
                            new LinearScalerNodeConfiguration
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