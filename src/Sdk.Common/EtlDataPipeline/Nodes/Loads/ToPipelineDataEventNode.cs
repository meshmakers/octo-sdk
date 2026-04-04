using Meshmakers.Octo.Common.DistributionEventHub.Services;
using Meshmakers.Octo.Communication.Contracts.MessageObjects;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Loads;

/// <summary>
/// Configuration for the distribution event hub node
/// </summary>
[NodeName("ToPipelineDataEvent", 1)]
public record ToPipelineDataEventNodeConfiguration : SourceTargetPathNodeConfiguration
{
    /// <summary>
    /// Gets or sets the RtId (OctoObjectId) of the target pipeline to route the data to.
    /// Must be a pipeline within the same DataFlow.
    /// </summary>
    public string TargetPipelineRtEntityId { get; set; } = string.Empty;
}

/// <summary>
/// Publishes the target object to the distribution event hub
/// </summary>
[NodeConfiguration(typeof(ToPipelineDataEventNodeConfiguration))]
// ReSharper disable once ClassNeverInstantiated.Global
public class ToPipelineDataEventNode(NodeDelegate next, IEtlContext adapterEtlContext, IDistributionEventHubService distributionEventHubService) : IPipelineNode
{
    /// <inheritdoc />
    public async Task ProcessObjectAsync(IDataContext dataContext, INodeContext nodeContext)
    {
        var c = nodeContext.GetNodeConfiguration<ToPipelineDataEventNodeConfiguration>();

        // if we don't define a timeout here, we will wait until the message is sent which can take quite a long time
        // when we don't have a connection to the event hub.
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // We transform the data context so that only the target object is sent to the event hub
        var o = dataContext.GetComplexObjectByPath<JToken>(c.Path);

        var target = new JObject();
        if (o != null)
        {
            target.ReplaceNested(c.TargetPath, o);
        }

        var s = JsonConvert.SerializeObject(target);

        var message = new PipelineDataReceived
        {
            TenantId = adapterEtlContext.TenantId,
            DataFlowRtId = adapterEtlContext.DataFlowRtId,
            PipelineRtEntityId = adapterEtlContext.PipelineRtEntityId,
            Value = s,
            TransactionStartedDateTime = adapterEtlContext.TransactionStartedDateTime,
            ExternalReceivedDateTime = adapterEtlContext.ExternalReceivedDateTime
        };

        if (string.IsNullOrEmpty(c.TargetPipelineRtEntityId))
        {
            throw DataPipelineException.MissingRequiredConfiguration("ToPipelineDataEvent", "targetPipelineRtEntityId");
        }

        var exchangeName =
            $"octo::com::dataflow-{adapterEtlContext.TenantId.ToLower()}-{adapterEtlContext.DataFlowRtId.ToString()?.ToLower()}";

        await distributionEventHubService.SendToExchangeAsync(exchangeName, c.TargetPipelineRtEntityId, message,
            cts.Token);

        await next(dataContext, nodeContext);
    }
}