using FakeItEasy;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Sdk.Common.Adapters;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Debugger;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;
using Sdk.Common.Tests.TestData;
using Sdk.Common.Tests.TestData.Dto;
using Xunit.Abstractions;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Control;

public class ForEachNodeTests(NodeFixture fixture, ITestOutputHelper testOutputHelper)
    : IClassFixture<NodeFixture>
{
    private DataContext PrepareTest(ForEachNodeConfiguration forEachNodeConfiguration, IPipelineDebugger? debugger = null)
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext(
            fixture.Services.BuildServiceProvider(), logger, null, debugger)
        {
            Current = JObject.FromObject(Generator.GenerateOrder())
        };
        dataContext.RegisterNode("ForEach", 0, forEachNodeConfiguration);
        return dataContext;
    }

    [Fact]
    public async Task ProcessObjectAsync_OK()
    {
        ForEachNodeConfiguration forEachNodeConfiguration = new()
        {
            Path = "$.Items",
            TargetPath = "$.Result",
            Transformations = new List<NodeConfiguration>
            {
                new TestNodeConfiguration()
            }
        };

        var testCounter = A.Fake<ITestCounter>();
        fixture.Services.AddSingleton(testCounter);
        A.CallTo(() => testCounter.GetNext()).Returns(0);

        var dataContext = PrepareTest(forEachNodeConfiguration);


        var fn = A.Fake<NodeDelegate>();
        var testee = new ForEachNode(fn);

        await testee.ProcessObjectAsync(dataContext);

        A.CallTo(() => testCounter.GetNext()).MustHaveHappened(3, Times.Exactly);
        A.CallTo(() => fn.Invoke(dataContext)).MustHaveHappenedOnceExactly();
        Assert.NotNull(dataContext.Current);
        Assert.Equal(3, dataContext.GetSimpleArrayValueByPath<int>("$.Result")?.Count());
    }

    [Fact]
    public async Task ProcessObjectAsync_SourceNull_OK()
    {
        ForEachNodeConfiguration forEachNodeConfiguration = new()
        {
            Path = "$.ItemsNotExisting",
            TargetPath = "$.Result",
            Transformations = new List<NodeConfiguration>
            {
                new TestNodeConfiguration()
            }
        };

        var testCounter = A.Fake<ITestCounter>();
        fixture.Services.AddSingleton(testCounter);
        A.CallTo(() => testCounter.GetNext()).Returns(0);
        
        var dataContext = PrepareTest(forEachNodeConfiguration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new ForEachNode(fn);

        await testee.ProcessObjectAsync(dataContext);

        A.CallTo(() => testCounter.GetNext()).MustNotHaveHappened();
        A.CallTo(() => fn.Invoke(dataContext)).MustHaveHappenedOnceExactly();
        Assert.NotNull(dataContext.Current);
        Assert.Equal(0, dataContext.GetSimpleArrayValueByPath<int>("$.Result")?.Count());
    }
    
    [Fact]
    public async Task ProcessObjectAsync_Output_OK()
    {
        fixture.UseXUnitLoggerFactory(testOutputHelper);
        var serviceProvider = fixture.Services.BuildServiceProvider();

        ForEachNodeConfiguration forEachNodeConfiguration = new()
        {
            Path = "$.Items",
            TargetPath = "$.Result",
            Transformations = new List<NodeConfiguration>
            {
                new TestNodeConfiguration
                {
                    TargetPath = "$.Output0"
                },
                new TestNodeConfiguration
                {
                    TargetPath = "$.Output1"
                }
            }
        };

        var testCounter = A.Fake<ITestCounter>();
        fixture.Services.AddSingleton(testCounter);
        A.CallTo(() => testCounter.GetNext()).Returns(0);

        var debugger = new DefaultPipelineDebugger(serviceProvider.GetRequiredService<ILoggerFactory>());
        var pipelineExecutionId = Guid.NewGuid();
        var pipelineEntityId = new RtEntityId("System.Communication/EdgePipeline", OctoObjectId.GenerateNewId());
        
        debugger.RegisterPipelineRtEntityId(pipelineEntityId, pipelineExecutionId);
        var dataContext = PrepareTest(forEachNodeConfiguration, debugger);

        var fn = A.Fake<NodeDelegate>();
        var testee = new ForEachNode(fn);

        await testee.ProcessObjectAsync(dataContext);

        var debugInfo = debugger.GetDebugInformation();
        
    }
}