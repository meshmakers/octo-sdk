using FakeItEasy;
using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.DataPipeline;
using Meshmakers.Octo.Sdk.Common.DataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.DataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.DataPipeline.Nodes.Transforms;
using Sdk.Common.Tests.Fixtures;

namespace Sdk.Common.Tests.DataPipeline.Configuration;

public class YamlPipelineConfigurationSerializerTests
    : IClassFixture<ServiceCollectionFixture>
{

    [Fact]
    public async Task Serialize_OK()
    {
        PipelineConfigurationRoot configurationRoot = new PipelineConfigurationRoot();
        configurationRoot.Transforms ??= new List<TransformConfigurationNode>();
        configurationRoot.Transforms.Add(new TransformByPathConfigurationNode
        {
            Transforms = new List<TransformPathPropertyConfigurationNode>
            {
                new()
                {
                    TargetPropertyName = "$.CustomerName",
                    ValueType = AttributeValueTypesDto.String
                }
            }
        });

        var nodeLookupService = A.Fake<INodeLookupService>();
        string? t;
        Type? v;
        A.CallTo(() => nodeLookupService.TryGetNodeQualifiedName(A<Type>._, out t))
            .Returns(true)
            .AssignsOutAndRefParameters("TransformObject@1");
        A.CallTo(() => nodeLookupService.TryGetConfigurationNodeType(A<string>._, out v))
            .Returns(true)
            .AssignsOutAndRefParameters(typeof(TransformByPathTransformNode));
        
        var serializer = new YamlPipelineConfigurationSerializer(nodeLookupService);
        using (var memoryStream = new MemoryStream())
        {
            var streamWriter = new StreamWriter(memoryStream);
            await serializer.SerializeAsync(streamWriter, configurationRoot);
            await streamWriter.FlushAsync();
            
            memoryStream.Position = 0;
            
            using var streamReader = new StreamReader(memoryStream);
            var s = await streamReader.ReadToEndAsync();

            memoryStream.Position = 0;

            var copy = await serializer.DeserializeAsync(memoryStream);
            Assert.NotNull(copy);
            Assert.Equal(1, copy.Transforms?.Count);
        }
    }
}