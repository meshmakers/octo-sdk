using FakeItEasy;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sdk.Common.Tests.EtlDataPipeline;

public class EtlContextAccessorTests
{
    private class DummyNodeConfiguration : UnitTestNodeConfiguration
    {
    }

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
        public bool DidRun { get; set; } = false;
    }

    private class DummyNodeWithoutContextConfiguration : UnitTestNodeConfiguration
    {
    }


    private interface IUnitTestContext : IEtlContext
    {
        DummyNodeWithContext? Node { get; set; }
    }

    private class UnitTestContext : IUnitTestContext
    {
        public DummyNodeWithContext? Node { get; set; }
        public string TenantId { get; } = Guid.NewGuid().ToString();
        public IDictionary<string, object?> Properties { get; } = new Dictionary<string, object?>();
    }

    [Fact]
    public async Task RunDataPipeline_NodeWithoutDependencies_Runs()
    {
        var builder = new ServiceCollection()
            .AddSingleton(A.Fake<ILoggerFactory>())
            .AddDataPipeline()
            .RegisterNode<DummyNodeWithoutContext>();

        var services = builder.Services.BuildServiceProvider();
        var c = new DefaultEtlContext("tenantId", new Dictionary<string, object?>());

        var orchestrator = services.GetRequiredService<IEtlDataOrchestrator>();

        var nodeConfig = new DummyNodeWithoutContextConfiguration();
        var pipelineConfigurationRoot = new PipelineConfigurationRoot { Transformations = new[] { nodeConfig } };


        await orchestrator.ExecutePipelineAsync<IEtlContext>(pipelineConfigurationRoot, c);

        
        Assert.True(nodeConfig.DidRun);
    }

    [Fact]
    public async Task RunDataPipeline_WithContext_RunsAndUsesContext()
    {
        var builder = new ServiceCollection()
            .AddSingleton(A.Fake<ILoggerFactory>())
            .AddDataPipeline()
            .RegisterEtlContext<IUnitTestContext>()
            .RegisterNode<DummyNodeWithContext>();

        var services = builder.Services.BuildServiceProvider();
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