using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sdk.Common.Tests.Fixtures;

namespace Sdk.Common.Tests.EtlDataPipeline;

public class EtlContextAccessorTests(DataPipelineFixture fixture) : IClassFixture<DataPipelineFixture>
{
    private class DummyNodeConfiguration : UnitTestNodeConfiguration;

    [Node("DummyNode", 1, typeof(DummyNodeConfiguration))]
    private class DummyNodeWithContext(NodeDelegate next, IUnitTestContext context) : IPipelineNode
    {
        public async Task ProcessObjectAsync(IDataContext dataContext)
        {
            context.Node = this;
            var c = dataContext.GetNodeConfiguration<DummyNodeConfiguration>();
            c.DidRun = true;
            await next(dataContext);
        }
    }


    [Node("DummyNodeWithoutContext", 1, typeof(DummyNodeWithoutContextConfiguration))]
    private class DummyNodeWithoutContext(NodeDelegate next) : IPipelineNode
    {
        public Task ProcessObjectAsync(IDataContext dataContext)
        {
            var c = dataContext.GetNodeConfiguration<DummyNodeWithoutContextConfiguration>();
            c.DidRun = true;
            return next(dataContext);
        }
    }

    private class UnitTestNodeConfiguration : NodeConfiguration
    {
        public bool DidRun { get; set; }
    }

    private class DummyNodeWithoutContextConfiguration : UnitTestNodeConfiguration;


    private interface IUnitTestContext : IEtlContext
    {
        DummyNodeWithContext? Node { get; set; }
    }

    private class UnitTestContext : IUnitTestContext
    {
        public DummyNodeWithContext? Node { get; set; }
        public string TenantId { get; } = Guid.NewGuid().ToString();
        public RtEntityId PipelineRtEntityId { get; } = default;
        public DateTime? ExternalReceivedDateTime { get; } = null;
        public IDictionary<string, object?> Properties { get; } = new Dictionary<string, object?>();

        public DateTime TransactionStartedDateTime { get; } = DateTime.UtcNow;
    }

    [Fact]
    public async Task RunDataPipeline_NodeWithoutDependencies_Runs()
    {
        fixture.DataPipelineBuilder
            .RegisterNode<DummyNodeWithoutContext>();

        var services = fixture.Services.BuildServiceProvider();
        var c = new DefaultEtlContext("tenantId", new RtEntityId("System.Communication/EdgeAdapter", OctoObjectId.GenerateNewId()), DateTime.UtcNow, 
            null, new Dictionary<string, object?>());

        var orchestrator = services.GetRequiredService<IEtlDataOrchestrator>();

        var nodeConfig = new DummyNodeWithoutContextConfiguration();
        var pipelineConfigurationRoot = new PipelineConfigurationRoot { Transformations = new[] { nodeConfig } };


        await orchestrator.ExecutePipelineAsync<IEtlContext>(pipelineConfigurationRoot, c);

        
        Assert.True(nodeConfig.DidRun);
    }

    [Fact]
    public async Task RunDataPipeline_WithContext_RunsAndUsesContext()
    {
        fixture.DataPipelineBuilder
            .RegisterEtlContext<IUnitTestContext>()
            .RegisterNode<DummyNodeWithContext>();

        var services = fixture.Services.BuildServiceProvider();
        var c = new UnitTestContext();

        var orchestrator = services.GetRequiredService<IEtlDataOrchestrator>();

        var nodeConfig = new DummyNodeConfiguration();
        var pipelineConfig = new PipelineConfigurationRoot { Transformations = new[] { nodeConfig } };

        
        await orchestrator.ExecutePipelineAsync<IUnitTestContext>(
            pipelineConfig, c);


        Assert.NotNull(c.Node);
        Assert.True(nodeConfig.DidRun);
    }
}