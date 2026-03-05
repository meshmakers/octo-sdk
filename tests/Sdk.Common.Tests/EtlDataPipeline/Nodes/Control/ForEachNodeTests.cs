using FakeItEasy;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Sdk.Common;
using Meshmakers.Octo.Sdk.Common.Adapters;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Debugger;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;
using Meshmakers.Octo.Sdk.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;
using Sdk.Common.Tests.TestData;
using Sdk.Common.Tests.TestData.Dto;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Control;

public class ForEachNodeTests(NodeFixture fixture, ITestOutputHelper testOutputHelper)
    : IClassFixture<NodeFixture>
{
    private (DataContext, INodeContext) PrepareTest(ForEachNodeConfiguration forEachNodeConfiguration,
        IPipelineDebugger? debugger = null)
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(Generator.GenerateOrder())
        };
        var rootNodeContext =
            NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("ForEach", 0, forEachNodeConfiguration, dataContext);
        return (dataContext, nodeContext);
    }

    [Fact]
    public async Task ProcessObjectAsync_OK()
    {
        ForEachNodeConfiguration forEachNodeConfiguration = new()
        {
            Path = "$.Items",
            IterationPath = "$.Items",
            TargetPath = "$.Result",
            Transformations = new List<NodeConfiguration>
            {
                new TestNodeConfiguration
                {
                    TargetPath =  "$.key",
                }
            }
        };

        var testCounter = A.Fake<ITestCounter>();
        fixture.Services.AddSingleton(testCounter);
        A.CallTo(() => testCounter.GetNext()).Returns(0);

        var (dataContext, nodeContext) = PrepareTest(forEachNodeConfiguration);


        var fn = A.Fake<NodeDelegate>();
        var testee = new ForEachNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => testCounter.GetNext()).MustHaveHappened(3, Times.Exactly);
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.NotNull(dataContext.Current);
        Assert.Equal(3, dataContext.GetSimpleArrayValueByPath<int>("$.Result")?.Count());
    }

    [Fact]
    public async Task ProcessObjectAsync_SourceNull_Fail()
    {
        ForEachNodeConfiguration forEachNodeConfiguration = new()
        {
            Path = "$.ItemsNotExisting",
            IterationPath = "$.ItemsNotExisting",
            TargetPath = "$.Result",
            Transformations = new List<NodeConfiguration>
            {
                new TestNodeConfiguration()
            }
        };

        var testCounter = A.Fake<ITestCounter>();
        fixture.Services.AddSingleton(testCounter);
        A.CallTo(() => testCounter.GetNext()).Returns(0);

        var (dataContext, nodeContext) = PrepareTest(forEachNodeConfiguration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new ForEachNode(fn);

        await Assert.ThrowsAsync<PipelineExecutionException>(async () =>
            await testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_NotAnArray_Fail()
    {
        ForEachNodeConfiguration forEachNodeConfiguration = new()
        {
            Path = "$.ItemsNotExisting",
            IterationPath = "$.ItemsNotExisting",
            TargetPath = "$.Result",
            Transformations = new List<NodeConfiguration>
            {
                new TestNodeConfiguration()
            }
        };

        var testCounter = A.Fake<ITestCounter>();
        fixture.Services.AddSingleton(testCounter);
        A.CallTo(() => testCounter.GetNext()).Returns(0);

        var (dataContext, nodeContext) = PrepareTest(forEachNodeConfiguration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new ForEachNode(fn);

        await Assert.ThrowsAsync<PipelineExecutionException>(async () =>
            await testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_Output_OK()
    {
        fixture.UseXUnitLoggerFactory(testOutputHelper);
        var serviceProvider = fixture.Services.BuildServiceProvider();

        ForEachNodeConfiguration forEachNodeConfiguration = new()
        {
            Path = "$.Items",
            IterationPath = "$.Items",
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
        var (dataContext, nodeContext) = PrepareTest(forEachNodeConfiguration, debugger);

        var fn = A.Fake<NodeDelegate>();
        var testee = new ForEachNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        var debugInfo = debugger.GetDebugInformation();
    }

    [Fact]
    public async Task ProcessObjectAsync_IterationPath_WithReplace_OK()
    {
        fixture.UseXUnitLoggerFactory(testOutputHelper);
        var serviceProvider = fixture.Services.BuildServiceProvider();

        ForEachNodeConfiguration forEachNodeConfiguration = new()
        {
            IterationPath = "$.Items",
            DocumentMode = DocumentModes.Replace,
            TargetPath = "$.Result",
            Transformations = new List<NodeConfiguration>
            {
                new TestNodeConfiguration
                {
                    TargetPath = "$.key.Output0"
                },
                new TestNodeConfiguration
                {
                    TargetPath = "$.key.Output1"
                }
            }
        };

        var testCounter = A.Fake<ITestCounter>();
        fixture.Services.AddSingleton(testCounter);
        A.CallTo(() => testCounter.GetNext()).ReturnsNextFromSequence(0, 1, 2, 3, 4, 5);

        var debugger = new DefaultPipelineDebugger(serviceProvider.GetRequiredService<ILoggerFactory>());
        var pipelineExecutionId = Guid.NewGuid();
        var pipelineEntityId = new RtEntityId("System.Communication/EdgePipeline", OctoObjectId.GenerateNewId());

        debugger.RegisterPipelineRtEntityId(pipelineEntityId, pipelineExecutionId);
        var (dataContext, nodeContext) = PrepareTest(forEachNodeConfiguration, debugger);

        var fn = A.Fake<NodeDelegate>();
        var testee = new ForEachNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);


        var result = dataContext.GetSimpleArrayValueByPath<JToken>("$.Result")?.ToList();
        Assert.Equal(3, result?.Count);
        Assert.NotNull(result?[0]?["Output0"]);
        Assert.NotNull(result[0]?["Output1"]);
        Assert.NotNull(result[1]?["Output0"]);
        Assert.NotNull(result[1]?["Output1"]);
        Assert.NotNull(result[2]?["Output0"]);
        Assert.NotNull(result[2]?["Output1"]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(-1)]
    public async Task ProcessObjectAsync_WithMaxDegreeOfParallelism_AllItemsProcessed(int maxDop)
    {
        ForEachNodeConfiguration forEachNodeConfiguration = new()
        {
            Path = "$.Items",
            IterationPath = "$.Items",
            TargetPath = "$.Result",
            MaxDegreeOfParallelism = maxDop,
            Transformations = new List<NodeConfiguration>
            {
                new TestNodeConfiguration
                {
                    TargetPath = "$.key",
                }
            }
        };

        var testCounter = A.Fake<ITestCounter>();
        fixture.Services.AddSingleton(testCounter);
        A.CallTo(() => testCounter.GetNext()).Returns(0);

        var (dataContext, nodeContext) = PrepareTest(forEachNodeConfiguration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new ForEachNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => testCounter.GetNext()).MustHaveHappened(3, Times.Exactly);
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.NotNull(dataContext.Current);
        Assert.Equal(3, dataContext.GetSimpleArrayValueByPath<int>("$.Result")?.Count());
    }

    [Fact]
    public async Task ProcessObjectAsync_MaxDegreeOfParallelism_LimitsConcurrency()
    {
        var concurrencyTracker = new ConcurrencyTracker();
        fixture.Services.AddSingleton(concurrencyTracker);

        ForEachNodeConfiguration forEachNodeConfiguration = new()
        {
            Path = "$.Items",
            IterationPath = "$.Items",
            TargetPath = "$.Result",
            MaxDegreeOfParallelism = 1,
            Transformations = new List<NodeConfiguration>
            {
                new DelayedTestNodeConfiguration
                {
                    TargetPath = "$.key",
                }
            }
        };

        var (dataContext, nodeContext) = PrepareTest(forEachNodeConfiguration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new ForEachNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        Assert.Equal(1, concurrencyTracker.MaxConcurrent);
        Assert.Equal(3, concurrencyTracker.TotalExecutions);
        Assert.Equal(3, dataContext.GetSimpleArrayValueByPath<int>("$.Result")?.Count());
    }

    /// <summary>
    /// Reproduces the bug: $.full is stored in _sharedData but many pipeline nodes
    /// access dataContext.Current.SelectToken("$.full.xxx") directly, bypassing
    /// the shared data resolution. This simulates what real nodes like
    /// FormatStringNode, SetPrimitiveValueNode, MathNode etc. do.
    /// </summary>
    [Fact]
    public async Task ProcessObjectAsync_FullDocumentPath_AccessibleViaCurrentSelectToken()
    {
        ForEachNodeConfiguration forEachNodeConfiguration = new()
        {
            Path = "$",
            IterationPath = "$.Items",
            TargetPath = "$.Result",
            MaxDegreeOfParallelism = 1,
            Transformations = new List<NodeConfiguration>
            {
                new FullDocAccessTestNodeConfiguration
                {
                    SourcePath = "$.full.InvoiceNumber",
                    TargetPath = "$.key",
                }
            }
        };

        fixture.Services.AddSingleton<IFullDocAccessResult>(new FullDocAccessResult());

        var (dataContext, nodeContext) = PrepareTest(forEachNodeConfiguration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new ForEachNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // All 3 iterations should have found $.full.InvoiceNumber via Current.SelectToken
        var result = dataContext.GetSimpleArrayValueByPath<JToken>("$.Result")?.ToList();
        Assert.Equal(3, result?.Count);

        // The critical assertion: each result must contain the ACTUAL InvoiceNumber value,
        // not a JValue(null). If $.full is missing from Current, SelectToken returns null
        // and SetValueByPath writes JValue.CreateNull() instead of the real value.
        var invoiceNumber = dataContext.GetSimpleValueByPath<int>("$.InvoiceNumber");
        foreach (var item in result!)
        {
            // item should be the actual InvoiceNumber, not a JValue(null)
            var jValue = Assert.IsType<JValue>(item);
            Assert.NotNull(jValue.Value);
            Assert.Equal(invoiceNumber, jValue.Value<int>());
        }
    }
}