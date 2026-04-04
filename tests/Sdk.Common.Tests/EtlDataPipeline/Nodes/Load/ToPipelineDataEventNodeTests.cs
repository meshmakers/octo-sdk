using FakeItEasy;
using Meshmakers.Octo.Common.DistributionEventHub.Services;
using Meshmakers.Octo.Communication.Contracts.MessageObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Loads;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Load;

public class ToPipelineDataEventNodeTests(ServiceCollectionFixture fixture)
    : IClassFixture<ServiceCollectionFixture>
{
    private static readonly string TestTenantId = "test-tenant";
    private static readonly OctoObjectId TestDataFlowRtId = OctoObjectId.GenerateNewId();
    private static readonly string TestTargetPipelineId = OctoObjectId.GenerateNewId().ToString()!;

    [Fact]
    public async Task ProcessObjectAsync_SendsToExchange_CallsNext()
    {
        // Arrange
        var config = new ToPipelineDataEventNodeConfiguration
        {
            Path = "$",
            TargetPath = "$",
            TargetPipelineRtEntityId = TestTargetPipelineId
        };
        var testData = new { temperature = 42.5, sensor = "T1" };
        var (dataContext, nodeContext) = PrepareTest(config, testData);
        var distributionEventHubService = A.Fake<IDistributionEventHubService>();
        A.CallTo(() => distributionEventHubService.SendToExchangeAsync(
                A<string>._, A<string>._, A<PipelineDataReceived>._, A<CancellationToken?>._))
            .Returns(Task.FromResult(Task.CompletedTask));
        var etlContext = CreateEtlContext();
        var fn = A.Fake<NodeDelegate>();

        var testee = new ToPipelineDataEventNode(fn, etlContext, distributionEventHubService);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => distributionEventHubService.SendToExchangeAsync(
            A<string>._,
            A<string>._,
            A<PipelineDataReceived>._,
            A<CancellationToken?>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ProcessObjectAsync_SendsToCorrectExchangeAndRoutingKey()
    {
        // Arrange
        var config = new ToPipelineDataEventNodeConfiguration
        {
            Path = "$",
            TargetPath = "$",
            TargetPipelineRtEntityId = TestTargetPipelineId
        };
        var (dataContext, nodeContext) = PrepareTest(config, new { value = 1 });
        var distributionEventHubService = A.Fake<IDistributionEventHubService>();
        var etlContext = CreateEtlContext();
        string? capturedExchangeName = null;
        string? capturedRoutingKey = null;

        A.CallTo(() => distributionEventHubService.SendToExchangeAsync(
                A<string>._, A<string>._, A<PipelineDataReceived>._, A<CancellationToken?>._))
            .Invokes((string exchangeName, string routingKey, PipelineDataReceived _, CancellationToken? _) =>
            {
                capturedExchangeName = exchangeName;
                capturedRoutingKey = routingKey;
            })
            .Returns(Task.FromResult(Task.CompletedTask));

        var fn = A.Fake<NodeDelegate>();
        var testee = new ToPipelineDataEventNode(fn, etlContext, distributionEventHubService);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        Assert.NotNull(capturedExchangeName);
        Assert.Contains(TestTenantId, capturedExchangeName);
        Assert.Contains(TestDataFlowRtId.ToString()!.ToLower(), capturedExchangeName);
        Assert.StartsWith("octo::com::dataflow-", capturedExchangeName);
        Assert.Equal(TestTargetPipelineId, capturedRoutingKey);
    }

    [Fact]
    public async Task ProcessObjectAsync_WithoutTargetPipelineRtEntityId_ThrowsException()
    {
        // Arrange
        var config = new ToPipelineDataEventNodeConfiguration
        {
            Path = "$",
            TargetPath = "$",
            TargetPipelineRtEntityId = ""
        };
        var (dataContext, nodeContext) = PrepareTest(config, new { value = 1 });
        var distributionEventHubService = A.Fake<IDistributionEventHubService>();
        var etlContext = CreateEtlContext();
        var fn = A.Fake<NodeDelegate>();

        var testee = new ToPipelineDataEventNode(fn, etlContext, distributionEventHubService);

        // Act & Assert
        await Assert.ThrowsAsync<DataPipelineException>(() =>
            testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_SendsCorrectTenantAndPipelineIds()
    {
        // Arrange
        var config = new ToPipelineDataEventNodeConfiguration
        {
            Path = "$",
            TargetPath = "$",
            TargetPipelineRtEntityId = TestTargetPipelineId
        };
        var (dataContext, nodeContext) = PrepareTest(config, new { value = 1 });
        var distributionEventHubService = A.Fake<IDistributionEventHubService>();
        var etlContext = CreateEtlContext();
        PipelineDataReceived? capturedMessage = null;

        A.CallTo(() => distributionEventHubService.SendToExchangeAsync(
                A<string>._, A<string>._, A<PipelineDataReceived>._, A<CancellationToken?>._))
            .Invokes((string _, string _, PipelineDataReceived msg, CancellationToken? _) => capturedMessage = msg)
            .Returns(Task.FromResult(Task.CompletedTask));

        var fn = A.Fake<NodeDelegate>();
        var testee = new ToPipelineDataEventNode(fn, etlContext, distributionEventHubService);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        Assert.NotNull(capturedMessage);
        Assert.Equal(TestTenantId, capturedMessage.TenantId);
        Assert.Equal(TestDataFlowRtId, capturedMessage.DataFlowRtId);
    }

    [Fact]
    public async Task ProcessObjectAsync_WithSubPath_SendsSubsetData()
    {
        // Arrange
        var config = new ToPipelineDataEventNodeConfiguration
        {
            Path = "$.nested",
            TargetPath = "$.output",
            TargetPipelineRtEntityId = TestTargetPipelineId
        };
        var testData = new { nested = new { key = "value" }, other = "ignored" };
        var (dataContext, nodeContext) = PrepareTest(config, testData);
        var distributionEventHubService = A.Fake<IDistributionEventHubService>();
        var etlContext = CreateEtlContext();
        PipelineDataReceived? capturedMessage = null;

        A.CallTo(() => distributionEventHubService.SendToExchangeAsync(
                A<string>._, A<string>._, A<PipelineDataReceived>._, A<CancellationToken?>._))
            .Invokes((string _, string _, PipelineDataReceived msg, CancellationToken? _) => capturedMessage = msg)
            .Returns(Task.FromResult(Task.CompletedTask));

        var fn = A.Fake<NodeDelegate>();
        var testee = new ToPipelineDataEventNode(fn, etlContext, distributionEventHubService);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        Assert.NotNull(capturedMessage);
        Assert.NotNull(capturedMessage.Value);

        var sentData = JObject.Parse(capturedMessage.Value);
        Assert.NotNull(sentData["output"]);
        Assert.Equal("value", sentData["output"]!["key"]?.Value<string>());

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
    }

    private (DataContext, INodeContext) PrepareTest(ToPipelineDataEventNodeConfiguration config,
        object? data = null)
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(data ?? new { })
        };

        var rootNodeContext =
            NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext =
            rootNodeContext.RegisterChildNode("ToPipelineDataEvent", 0, config, dataContext);

        return (dataContext, nodeContext);
    }

    private static IEtlContext CreateEtlContext()
    {
        var etlContext = A.Fake<IEtlContext>();
        A.CallTo(() => etlContext.TenantId).Returns(TestTenantId);
        A.CallTo(() => etlContext.DataFlowRtId).Returns(TestDataFlowRtId);
        A.CallTo(() => etlContext.PipelineRtEntityId).Returns(default(RtEntityId));
        A.CallTo(() => etlContext.TransactionStartedDateTime).Returns(DateTime.UtcNow);
        return etlContext;
    }
}
