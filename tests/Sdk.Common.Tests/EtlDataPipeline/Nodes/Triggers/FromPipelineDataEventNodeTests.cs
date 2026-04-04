using FakeItEasy;
using Meshmakers.Octo.Common.DistributionEventHub.Services;
using Meshmakers.Octo.Communication.Contracts.MessageObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Triggers;
using Microsoft.Extensions.DependencyInjection;
using Sdk.Common.Tests.Fixtures;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Triggers;

public class FromPipelineDataEventNodeTests(ServiceCollectionFixture fixture)
    : IClassFixture<ServiceCollectionFixture>
{
    [Fact]
    public async Task StartAsync_RegistersEventConsumer_WithCorrectExchangeAndRoutingKey()
    {
        // Arrange
        var eventHubControl = A.Fake<IEventHubControl>();
        var endpointHandle = A.Fake<EndpointHandle>();

        A.CallTo(() => eventHubControl.RegisterRoutedEventConsumer<PipelineDataReceived>(
            A<string>._, A<string>._, A<Func<PipelineDataReceived, Task>>._)).Returns(endpointHandle);

        var triggerContext = CreateTriggerContext();

        var testee = new FromPipelineDataEventNode(eventHubControl);

        // Act
        await testee.StartAsync(triggerContext);

        // Assert
        A.CallTo(() => eventHubControl.RegisterRoutedEventConsumer<PipelineDataReceived>(
            A<string>.That.Contains("octo::com::dataflow-"),
            A<string>._,
            A<Func<PipelineDataReceived, Task>>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task StartAsync_RegistersWithCorrectExchangeNameAndRoutingKey()
    {
        // Arrange
        var eventHubControl = A.Fake<IEventHubControl>();
        var endpointHandle = A.Fake<EndpointHandle>();
        string? capturedExchangeName = null;
        string? capturedRoutingKey = null;

        A.CallTo(() => eventHubControl.RegisterRoutedEventConsumer<PipelineDataReceived>(
                A<string>._, A<string>._, A<Func<PipelineDataReceived, Task>>._))
            .Invokes((string exchangeName, string routingKey, Func<PipelineDataReceived, Task> _) =>
            {
                capturedExchangeName = exchangeName;
                capturedRoutingKey = routingKey;
            })
            .Returns(endpointHandle);

        var dataFlowRtId = OctoObjectId.GenerateNewId();
        var pipelineRtEntityId = new RtEntityId(new RtCkId<CkTypeId>("System.Communication/DataFlow"), dataFlowRtId);
        var triggerContext = CreateTriggerContext("my-tenant", dataFlowRtId, pipelineRtEntityId);

        var testee = new FromPipelineDataEventNode(eventHubControl);

        // Act
        await testee.StartAsync(triggerContext);

        // Assert
        Assert.NotNull(capturedExchangeName);
        Assert.Contains("my-tenant", capturedExchangeName);
        Assert.Contains(dataFlowRtId.ToString()!.ToLower(), capturedExchangeName);
        Assert.NotNull(capturedRoutingKey);
        Assert.Equal(pipelineRtEntityId.RtId.ToString(), capturedRoutingKey);
    }

    [Fact]
    public async Task StopAsync_AfterStart_CompletesWithoutError()
    {
        // Arrange
        var eventHubControl = A.Fake<IEventHubControl>();

        var triggerContext = CreateTriggerContext();
        var testee = new FromPipelineDataEventNode(eventHubControl);
        await testee.StartAsync(triggerContext);

        // Act & Assert - should complete without throwing
        await testee.StopAsync(triggerContext);
    }

    [Fact]
    public async Task StopAsync_WithoutStart_DoesNotThrow()
    {
        // Arrange
        var eventHubControl = A.Fake<IEventHubControl>();
        var triggerContext = CreateTriggerContext();
        var testee = new FromPipelineDataEventNode(eventHubControl);

        // Act & Assert - should not throw
        await testee.StopAsync(triggerContext);
    }

    private ITriggerContext CreateTriggerContext(string tenantId = "test-tenant",
        OctoObjectId? dataFlowRtId = null, RtEntityId? pipelineRtEntityId = null)
    {
        var rtId = dataFlowRtId ?? OctoObjectId.GenerateNewId();
        var pipelineId = pipelineRtEntityId ??
                         new RtEntityId(new RtCkId<CkTypeId>("System.Communication/Pipeline"), OctoObjectId.GenerateNewId());
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext();

        var rootNodeContext =
            NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("FromPipelineDataEvent", 0,
            new FromPipelineDataEventNodeConfiguration(), dataContext);

        var triggerContext = A.Fake<ITriggerContext>();
        A.CallTo(() => triggerContext.NodeContext).Returns(nodeContext);
        A.CallTo(() => triggerContext.TenantId).Returns(tenantId);
        A.CallTo(() => triggerContext.DataFlowRtId).Returns(rtId);
        A.CallTo(() => triggerContext.PipelineRtEntityId).Returns(pipelineId);

        return triggerContext;
    }
}
