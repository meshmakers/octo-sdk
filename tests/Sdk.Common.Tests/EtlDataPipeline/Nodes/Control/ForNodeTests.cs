using FakeItEasy;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.Adapters;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Debugger;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;
using Sdk.Common.Tests.TestData;
using Sdk.Common.Tests.TestData.Dto;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Control;

public class ForNodeTests(NodeFixture fixture) : IClassFixture<NodeFixture>
{
    private (DataContext, INodeContext) PrepareTest(ForNodeConfiguration forNodeConfiguration,
        IPipelineDebugger? debugger = null)
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(Generator.GenerateOrder())
        };

        var rootNodeContext =
            NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext, debugger);
        var nodeContext = rootNodeContext.RegisterChildNode("For", 0, forNodeConfiguration, dataContext);
        return (dataContext, nodeContext);
    }

    [Fact]
    public async Task ProcessObjectAsync_OK()
    {
        ForNodeConfiguration forEachNodeConfiguration = new()
        {
            Count = 5,
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
        var testee = new ForNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => testCounter.GetNext()).MustHaveHappened(5, Times.Exactly);
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.NotNull(dataContext.Current);
        Assert.Equal(5, dataContext.GetSimpleArrayValueByPath<int>("$.Result")?.Count());
    }

    [Fact]
    public async Task ProcessObjectAsync_Debugger_InputCalls_OK()
    {
        ForNodeConfiguration forEachNodeConfiguration = new()
        {
            Count = 1,
            TargetPath = "$.Result",
            Transformations = new List<NodeConfiguration>
            {
                new TestNodeConfiguration()
            }
        };

        var debugger = A.Fake<IPipelineDebugger>();
        var testCounter = A.Fake<ITestCounter>();
        fixture.Services.AddSingleton(testCounter);
        A.CallTo(() => testCounter.GetNext()).Returns(0);

        var (dataContext, nodeContext) = PrepareTest(forEachNodeConfiguration, debugger);

        var fn = A.Fake<NodeDelegate>();
        var testee = new ForNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => debugger.LogInput(A<string>._, A<NodePath>._, A<string?>._, A<uint>._, A<JToken>._))
            .MustHaveHappened(4, Times.Exactly);
        A.CallTo(() => debugger.LogInput(A<string>._, "PipelineExecution", A<string?>._, A<uint>._, A<JToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => debugger.LogInput(A<string>._, "PipelineExecution/For", A<string?>._, A<uint>._, A<JToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(
                () => debugger.LogInput(A<string>._, "PipelineExecution/For/[0]", A<string?>._, A<uint>._, A<JToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() =>
                debugger.LogInput(A<string>._, "PipelineExecution/For/[0]/Test@1", A<string?>._, A<uint>._,
                    A<JToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ProcessObjectAsync_Debugger_OutputCalls_OK()
    {
        ForNodeConfiguration forEachNodeConfiguration = new()
        {
            Count = 1,
            TargetPath = "$.Result",
            Transformations = new List<NodeConfiguration>
            {
                new TestNodeConfiguration()
            }
        };

        var debugger = A.Fake<IPipelineDebugger>();
        var testCounter = A.Fake<ITestCounter>();
        fixture.Services.AddSingleton(testCounter);
        A.CallTo(() => testCounter.GetNext()).Returns(0);

        var (dataContext, nodeContext) = PrepareTest(forEachNodeConfiguration, debugger);

        var fn = A.Fake<NodeDelegate>();
        var testee = new ForNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => debugger.LogOutput(A<string>._, A<NodePath>._, A<string?>._, A<uint>._, A<JToken>._))
            .MustHaveHappened(2, Times.Exactly);
        A.CallTo(() =>
                debugger.LogOutput(A<string>._, "PipelineExecution/For/[0]", A<string?>._, A<uint>._, A<JToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() =>
                debugger.LogOutput(A<string>._, "PipelineExecution/For/[0]/Test@1", A<string?>._, A<uint>._,
                    A<JToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ProcessObjectAsync_Debugger_OutputCalls_MultipleElements_OK()
    {
        ForNodeConfiguration forEachNodeConfiguration = new()
        {
            Count = 1,
            TargetPath = "$.Result",
            Transformations = new List<NodeConfiguration>
            {
                new TestOutputNodeConfiguration(),
                new TestNodeConfiguration()
            }
        };

        var debugger = A.Fake<IPipelineDebugger>();
        var testCounter = A.Fake<ITestCounter>();
        fixture.Services.AddSingleton(testCounter);
        A.CallTo(() => testCounter.GetNext()).Returns(0);

        var (dataContext, nodeContext) = PrepareTest(forEachNodeConfiguration, debugger);

        var fn = A.Fake<NodeDelegate>();
        var testee = new ForNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => debugger.LogOutput(A<string>._, A<NodePath>._, A<string?>._, A<uint>._, A<JToken>._))
            .MustHaveHappened(3, Times.Exactly);
        A.CallTo(() =>
                debugger.LogOutput(A<string>._, "PipelineExecution/For/[0]", A<string?>._, A<uint>._, A<JToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() =>
                debugger.LogOutput(A<string>._, "PipelineExecution/For/[0]/TestOutput@1", A<string?>._, A<uint>._,
                    A<JToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() =>
                debugger.LogOutput(A<string>._, "PipelineExecution/For/[0]/Test@1", A<string?>._, A<uint>._,
                    A<JToken>._))
            .MustHaveHappenedOnceExactly();
    }
}