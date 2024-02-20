using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Sdk.Common.Tests.DataPipeline;
using Sdk.Common.Tests.Fixtures;

namespace Sdk.Common.Tests.EtlDataPipeline;

public class EtlDataOrchestratorTests(DataPipelineFixture fixture) : IClassFixture<DataPipelineFixture>
{
    [Fact]
    public async Task Serialize_OK()
    {
        var serviceProvider = fixture.Services.BuildServiceProvider();

        var dataOrchestrator = new EtlDataOrchestrator(serviceProvider, TestPipelineConfigurations.Test1,
            serviceProvider.GetRequiredService<INodeLookupService>());

        await dataOrchestrator.ExecutePipelineAsync();
    }
}