using Meshmakers.Octo.Common.DistributionEventHub.Services;
using Meshmakers.Octo.Communication.Contracts.MessageObjects;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.Services;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Triggers;

/// <summary>
/// Configuration for node FromPipelineDataEvent
/// </summary>
[NodeName("FromPipelineDataEvent", 1)]
public record FromPipelineDataEventNodeConfiguration : TriggerNodeConfiguration;


[NodeConfiguration(typeof(FromPipelineDataEventNodeConfiguration))]
// ReSharper disable once ClassNeverInstantiated.Global
internal class FromPipelineDataEventNode(IEventHubControl eventHubControl)
    : ITriggerPipelineNode
{
    private EndpointHandle? _endpointHandle;

    public Task StartAsync(ITriggerContext context)
    {
        var address =
            $"octo::com::{nameof(PipelineDataReceived).ToLower()}-{context.TenantId.ToLower()}-data-pipeline-{context.DataPipelineRtId.ToString()?.ToLower()}";

        _endpointHandle = eventHubControl.RegisterRoutedEventConsumer<PipelineDataReceived>(address,
            async message =>
            {
                if (message.Value == null)
                {
                    context.NodeContext.Warning("Received message with null value");
                    return;
                }

                var input = JToken.Parse(message.Value);
                await context.ExecuteAsync(new ExecutePipelineOptions(message.TransactionStartedDateTime)
                    { ExternalReceivedDateTime = message.ExternalReceivedDateTime }, input);
            });

        return Task.CompletedTask;
    }

    public async Task StopAsync(ITriggerContext context)
    {
        if (_endpointHandle != null)
        {
            await _endpointHandle.DisposeAsync();
        }
    }
}