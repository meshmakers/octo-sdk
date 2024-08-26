using MassTransit;
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
[NodeName("PublishToDistributionEventHub", 1)]
public class DistributionEventHubNodeConfiguration : NodeConfiguration;

/// <summary>
/// Publishes the target object to the distribution event hub
/// </summary>
[NodeConfiguration(typeof(DistributionEventHubNodeConfiguration))]
public class DistributionEventHubNode(NodeDelegate next, IAdapterEtlContext adapterEtlContext) : IPipelineNode
{
    /// <inheritdoc />
    public async Task ProcessObjectAsync(IDataContext dataContext)
    {
        var c = dataContext.GetNodeConfiguration<DistributionEventHubNodeConfiguration>();
        var distributionEventHubService = dataContext.GlobalServiceProvider.GetRequiredService<IDistributionEventHubService>();
        
        // if we don't define a timeout here, we will wait until the message is sent which can take quite a long time
        // when we don't have a connection to the event hub.
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var s = JsonConvert.SerializeObject(dataContext.Current);
        
        await distributionEventHubService.PublishAsync(new PipelineDataReceived
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