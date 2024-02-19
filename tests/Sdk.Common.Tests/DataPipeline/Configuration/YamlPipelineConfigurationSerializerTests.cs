using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.DataPipeline;
using Meshmakers.Octo.Sdk.Common.DataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.DataPipeline.Nodes.Objects;
using Sdk.Common.Tests.Fixtures;

namespace Sdk.Common.Tests.DataPipeline.Configuration;

public class YamlPipelineConfigurationSerializerTests
    : IClassFixture<ServiceCollectionFixture>
{

    [Fact]
    public async Task Serialize_OK()
    {
        PipelineConfigurationRoot configurationRoot = new PipelineConfigurationRoot();
        configurationRoot.TransformList ??= new List<ConfigurationNode>();
        configurationRoot.TransformList.Add(new AssignObjectConfigurationNode
        {
            TransformList = new List<AssignObjectTransformationNode>
            {
                new()
                {
                    Path = "$.Customer.Name",
                    Name = "CustomerName",
                    ValueType = AttributeValueTypesDto.String
                }
            }
        });
  
        var serializer = new YamlPipelineConfigurationSerializer();
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
            Assert.Equal(1, copy.TransformList?.Count);
        }
    }
}