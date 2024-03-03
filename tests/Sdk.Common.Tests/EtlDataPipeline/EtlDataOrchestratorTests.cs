using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;
using Sdk.Common.Tests.TestData;

namespace Sdk.Common.Tests.EtlDataPipeline;

public class EtlDataOrchestratorTests(DataPipelineFixture fixture) : IClassFixture<DataPipelineFixture>
{
    [Fact]
    public async Task ExecutePipelineAsync_OK()
    {
        var serviceProvider = fixture.Services.BuildServiceProvider();

        var dataOrchestrator = new EtlDataOrchestrator(serviceProvider,
            serviceProvider.GetRequiredService<INodeLookupService>());

        var r = await dataOrchestrator.ExecutePipelineAsync(TestPipelineConfigurations.Test1,
            new DefaultEtlContext("test1", OctoObjectId.GenerateNewId(), DateTime.UtcNow, null,
                new Dictionary<string, object?>()));

        Assert.NotNull(r);
        
        // Transformed from InvoiceNumber, but linear scaling was applied
        var jToken = (JToken) r;
        Assert.Equal(3, jToken.Count());
        Assert.Equal(760, jToken.SelectToken("$.InvoiceNumber"));

        // Transformed from Items
        var t = (JArray?) jToken.SelectToken("$.OrderItems");
        Assert.NotNull(t);
        Assert.Equal(3, t.Count());

        // This property was not excluded from source
        Assert.NotNull(jToken.SelectToken("$.InvoiceDate"));
    }
}