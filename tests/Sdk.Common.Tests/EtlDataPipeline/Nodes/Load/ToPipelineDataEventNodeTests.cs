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
    private static readonly OctoObjectId TestPipelineRtId = OctoObjectId.GenerateNewId();

    [Fact]
    public async Task ProcessObjectAsync_SendsDataToEventHub_CallsNext()
    {
        // Arrange
        var config = new ToPipelineDataEventNodeConfiguration
        {
            Path = "$",
            TargetPath = "$"
        };
        var testData = new { temperature = 42.5, sensor = "T1" };
        var (dataContext, nodeContext) = PrepareTest(config, testData);
        var distributionEventHubService = A.Fake<IDistributionEventHubService>();
        var etlContext = CreateEtlContext();
        var fn = A.Fake<NodeDelegate>();

        var testee = new ToPipelineDataEventNode(fn, etlContext, distributionEventHubService);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => distributionEventHubService.SendAsync(
            A<Uri>._,
            A<PipelineDataReceived>._,
            A<CancellationToken?>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ProcessObjectAsync_SendsCorrectUri()
    {
        // Arrange
        var config = new ToPipelineDataEventNodeConfiguration
        {
            Path = "$",
            TargetPath = "$"
        };
        var (dataContext, nodeContext) = PrepareTest(config, new { value = 1 });
        var distributionEventHubService = A.Fake<IDistributionEventHubService>();
        var etlContext = CreateEtlContext();
        Uri? capturedUri = null;

        A.CallTo(() => distributionEventHubService.SendAsync(
                A<Uri>._, A<PipelineDataReceived>._, A<CancellationToken?>._))
            .Invokes((Uri uri, PipelineDataReceived _, CancellationToken? _) => capturedUri = uri)
            .Returns(Task.CompletedTask);

        var fn = A.Fake<NodeDelegate>();
        var testee = new ToPipelineDataEventNode(fn, etlContext, distributionEventHubService);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        Assert.NotNull(capturedUri);
        Assert.Contains("pipelinedatareceived", capturedUri.ToString());
        Assert.Contains(TestTenantId, capturedUri.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_SendsCorrectTenantAndPipelineIds()
    {
        // Arrange
        var config = new ToPipelineDataEventNodeConfiguration
        {
            Path = "$",
            TargetPath = "$"
        };
        var (dataContext, nodeContext) = PrepareTest(config, new { value = 1 });
        var distributionEventHubService = A.Fake<IDistributionEventHubService>();
        var etlContext = CreateEtlContext();
        PipelineDataReceived? capturedMessage = null;

        A.CallTo(() => distributionEventHubService.SendAsync(
                A<Uri>._, A<PipelineDataReceived>._, A<CancellationToken?>._))
            .Invokes((Uri _, PipelineDataReceived msg, CancellationToken? _) => capturedMessage = msg)
            .Returns(Task.CompletedTask);

        var fn = A.Fake<NodeDelegate>();
        var testee = new ToPipelineDataEventNode(fn, etlContext, distributionEventHubService);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        Assert.NotNull(capturedMessage);
        Assert.Equal(TestTenantId, capturedMessage.TenantId);
        Assert.Equal(TestPipelineRtId, capturedMessage.DataPipelineRtId);
    }

    [Fact]
    public async Task ProcessObjectAsync_WithSubPath_SendsSubsetData()
    {
        // Arrange
        var config = new ToPipelineDataEventNodeConfiguration
        {
            Path = "$.nested",
            TargetPath = "$.output"
        };
        var testData = new { nested = new { key = "value" }, other = "ignored" };
        var (dataContext, nodeContext) = PrepareTest(config, testData);
        var distributionEventHubService = A.Fake<IDistributionEventHubService>();
        var etlContext = CreateEtlContext();
        PipelineDataReceived? capturedMessage = null;

        A.CallTo(() => distributionEventHubService.SendAsync(
                A<Uri>._, A<PipelineDataReceived>._, A<CancellationToken?>._))
            .Invokes((Uri _, PipelineDataReceived msg, CancellationToken? _) => capturedMessage = msg)
            .Returns(Task.CompletedTask);

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
        A.CallTo(() => etlContext.DataPipelineRtId).Returns(TestPipelineRtId);
        A.CallTo(() => etlContext.PipelineRtEntityId).Returns(default(RtEntityId));
        A.CallTo(() => etlContext.TransactionStartedDateTime).Returns(DateTime.UtcNow);
        return etlContext;
    }
}
