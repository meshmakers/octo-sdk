using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration.Serializer;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;
using Microsoft.Extensions.DependencyInjection;
using Sdk.Common.Tests.Fixtures;

namespace Sdk.Common.Tests.EtlDataPipeline.Configuration.Serializer;

public class JsonPipelineConfigurationSerializerTests(DataPipelineFixture dataPipelineFixture) : IClassFixture<DataPipelineFixture>
{
    [Fact]
    public async Task Serialize_OK()
    {
        PipelineConfigurationRoot configurationRoot = new PipelineConfigurationRoot();
        configurationRoot.Transformations ??= new List<NodeConfiguration>();
        configurationRoot.Transformations.Add(new SelectByPathNodeConfiguration
        {
            Transformations = new List<PathPropertyConfigurationNode>
            {
                new()
                {
                    TargetPropertyName = "$.CustomerName",
                }
            }
        });

        var serviceProvider = dataPipelineFixture.Services.BuildServiceProvider();
        var nodeQualifiedNameLookupService = serviceProvider.GetRequiredService<INodeQualifiedNameLookupService>(); 

        var serializer = new JsonPipelineConfigurationSerializer(nodeQualifiedNameLookupService);
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
            Assert.Equal(1, copy.Transformations?.Count);
        }
    }
}