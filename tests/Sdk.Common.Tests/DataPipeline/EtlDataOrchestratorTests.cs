using FakeItEasy;
using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.DataPipeline;
using Meshmakers.Octo.Sdk.Common.DataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.DataPipeline.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Sdk.Common.Tests.Fixtures;

namespace Sdk.Common.Tests.DataPipeline;

public class EtlDataOrchestratorTests(DataPipelineFixture fixture)
    : IClassFixture<DataPipelineFixture>
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