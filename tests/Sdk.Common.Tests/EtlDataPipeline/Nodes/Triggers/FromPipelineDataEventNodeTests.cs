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
    public async Task StartAsync_RegistersEventConsumer_WithCorrectAddress()
    {
        // Arrange
        var eventHubControl = A.Fake<IEventHubControl>();
        var endpointHandle = A.Fake<EndpointHandle>();

        A.CallTo(() => eventHubControl.RegisterRoutedEventConsumer<PipelineDataReceived>(
            A<string>._, A<Func<PipelineDataReceived, Task>>._)).Returns(endpointHandle);

        var triggerContext = CreateTriggerContext();

        var testee = new FromPipelineDataEventNode(eventHubControl);

        // Act
        await testee.StartAsync(triggerContext);

        // Assert
        A.CallTo(() => eventHubControl.RegisterRoutedEventConsumer<PipelineDataReceived>(
            A<string>.That.Contains("pipelinedatareceived"),
            A<Func<PipelineDataReceived, Task>>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task StartAsync_RegistersWithTenantAndPipelineInAddress()
    {
        // Arrange
        var eventHubControl = A.Fake<IEventHubControl>();
        var endpointHandle = A.Fake<EndpointHandle>();
        string? capturedAddress = null;

        A.CallTo(() => eventHubControl.RegisterRoutedEventConsumer<PipelineDataReceived>(
                A<string>._, A<Func<PipelineDataReceived, Task>>._))
            .Invokes((string address, Func<PipelineDataReceived, Task> _) => capturedAddress = address)
            .Returns(endpointHandle);

        var pipelineRtId = OctoObjectId.GenerateNewId();
        var triggerContext = CreateTriggerContext("my-tenant", pipelineRtId);

        var testee = new FromPipelineDataEventNode(eventHubControl);

        // Act
        await testee.StartAsync(triggerContext);

        // Assert
        Assert.NotNull(capturedAddress);
        Assert.Contains("my-tenant", capturedAddress);
        Assert.Contains(pipelineRtId.ToString()!.ToLower(), capturedAddress);
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
        OctoObjectId? pipelineRtId = null)
    {
        var rtId = pipelineRtId ?? OctoObjectId.GenerateNewId();
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext();

        var rootNodeContext =
            NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("FromPipelineDataEvent", 0,
            new FromPipelineDataEventNodeConfiguration(), dataContext);

        var triggerContext = A.Fake<ITriggerContext>();
        A.CallTo(() => triggerContext.NodeContext).Returns(nodeContext);
        A.CallTo(() => triggerContext.TenantId).Returns(tenantId);
        A.CallTo(() => triggerContext.DataPipelineRtId).Returns(rtId);

        return triggerContext;
    }
}
