using FakeItEasy;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Transforms;

public class PrintDebugNodeTests(ServiceCollectionFixture fixture)
    : IClassFixture<ServiceCollectionFixture>
{
    [Fact]
    public async Task ProcessObjectAsync_WithInformationSeverity_LogsInfo()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = new JObject
            {
                ["message"] = "Hello, World!"
            }
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("PrintDebug", 0, new PrintDebugNodeConfiguration
        {
            Severity = LoggerSeverity.Information
        }, dataContext);

        var fn = A.Fake<NodeDelegate>();
        var testee = new PrintDebugNode(fn);
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ProcessObjectAsync_WithDebugSeverity_LogsDebug()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = new JObject
            {
                ["value"] = 42
            }
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("PrintDebug", 0, new PrintDebugNodeConfiguration
        {
            Severity = LoggerSeverity.Debug
        }, dataContext);

        var fn = A.Fake<NodeDelegate>();
        var testee = new PrintDebugNode(fn);
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ProcessObjectAsync_WithWarningSeverity_LogsWarning()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = new JObject
            {
                ["warning"] = "Something might be wrong"
            }
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("PrintDebug", 0, new PrintDebugNodeConfiguration
        {
            Severity = LoggerSeverity.Warning
        }, dataContext);

        var fn = A.Fake<NodeDelegate>();
        var testee = new PrintDebugNode(fn);
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ProcessObjectAsync_WithErrorSeverity_LogsError()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = new JObject
            {
                ["error"] = "An error occurred"
            }
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("PrintDebug", 0, new PrintDebugNodeConfiguration
        {
            Severity = LoggerSeverity.Error
        }, dataContext);

        var fn = A.Fake<NodeDelegate>();
        var testee = new PrintDebugNode(fn);
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ProcessObjectAsync_WithNullCurrent_LogsNull()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = null
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("PrintDebug", 0, new PrintDebugNodeConfiguration
        {
            Severity = LoggerSeverity.Information
        }, dataContext);

        var fn = A.Fake<NodeDelegate>();
        var testee = new PrintDebugNode(fn);
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ProcessObjectAsync_WithDefaultSeverity_UsesInformation()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = new JObject
            {
                ["data"] = "test"
            }
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("PrintDebug", 0, new PrintDebugNodeConfiguration(), dataContext);

        var fn = A.Fake<NodeDelegate>();
        var testee = new PrintDebugNode(fn);
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ProcessObjectAsync_WithComplexObject_LogsStringRepresentation()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = new JObject
            {
                ["nested"] = new JObject
                {
                    ["array"] = new JArray { 1, 2, 3 },
                    ["value"] = "test"
                }
            }
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("PrintDebug", 0, new PrintDebugNodeConfiguration
        {
            Severity = LoggerSeverity.Debug
        }, dataContext);

        var fn = A.Fake<NodeDelegate>();
        var testee = new PrintDebugNode(fn);
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ProcessObjectAsync_WithPrimitiveValue_LogsValue()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = 42
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("PrintDebug", 0, new PrintDebugNodeConfiguration
        {
            Severity = LoggerSeverity.Information
        }, dataContext);

        var fn = A.Fake<NodeDelegate>();
        var testee = new PrintDebugNode(fn);
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ProcessObjectAsync_WithInvalidSeverity_ThrowsArgumentOutOfRangeException()
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = new JObject()
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("PrintDebug", 0, new PrintDebugNodeConfiguration
        {
            Severity = (LoggerSeverity)999
        }, dataContext);

        var fn = A.Fake<NodeDelegate>();
        var testee = new PrintDebugNode(fn);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => testee.ProcessObjectAsync(dataContext, nodeContext));
    }
}
