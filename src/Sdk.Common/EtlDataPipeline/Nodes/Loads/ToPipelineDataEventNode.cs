using Meshmakers.Octo.Common.DistributionEventHub.Services;
using Meshmakers.Octo.Communication.Contracts.MessageObjects;
using Meshmakers.Octo.Sdk.Common.Adapters;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Loads;

/// <summary>
/// Configuration for the distribution event hub node
/// </summary>
[NodeName("ToPipelineDataEvent", 1)]
public record ToPipelineDataEventNodeConfiguration : NodeConfiguration;

/// <summary>
/// Publishes the target object to the distribution event hub
/// </summary>
[NodeConfiguration(typeof(ToPipelineDataEventNodeConfiguration))]
// ReSharper disable once ClassNeverInstantiated.Global
public class ToPipelineDataEventNode(NodeDelegate next, IEtlContext adapterEtlContext) : IPipelineNode
{
    /// <inheritdoc />
    public async Task ProcessObjectAsync(IDataContext dataContext)
    {
        var distributionEventHubService =
            dataContext.GlobalServiceProvider.GetRequiredService<IDistributionEventHubService>();

        // if we don't define a timeout here, we will wait until the message is sent which can take quite a long time
        // when we don't have a connection to the event hub.
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var uri = new Uri(
            $"queue:octo::com::{nameof(PipelineDataReceived).ToLower()}-{adapterEtlContext.TenantId.ToLower()}-data-pipeline-{adapterEtlContext.DataPipelineRtId.ToString()?.ToLower()}");

        var s = JsonConvert.SerializeObject(dataContext.Current);

        await distributionEventHubService.SendAsync(uri, new PipelineDataReceived
        {
            TenantId = adapterEtlContext.TenantId,
            DataPipelineRtId = adapterEtlContext.DataPipelineRtId,
            PipelineRtEntityId = adapterEtlContext.PipelineRtEntityId,
            Value = s,
            TransactionStartedDateTime = adapterEtlContext.TransactionStartedDateTime,
            ExternalReceivedDateTime = adapterEtlContext.ExternalReceivedDateTime
        }, cts.Token);

        await next(dataContext);
    }
}