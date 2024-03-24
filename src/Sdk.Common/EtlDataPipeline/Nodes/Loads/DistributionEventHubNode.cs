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
public class DistributionEventHubNodeConfiguration : NodeConfiguration;

/// <summary>
/// Publishes the target object to the distribution event hub
/// </summary>
[Node("PublishToDistributionEventHub", 1, typeof(DistributionEventHubNodeConfiguration))]
public class DistributionEventHubNode(NodeDelegate next, IAdapterEtlContext adapterEtlContext) : IPipelineNode
{
    /// <inheritdoc />
    public async Task ProcessObjectAsync(IDataContext dataContext)
    {
        var c = dataContext.GetNodeConfiguration<DistributionEventHubNodeConfiguration>();
        var distributionEventHubService = dataContext.GlobalServiceProvider.GetRequiredService<IDistributionEventHubService>();

        var s = JsonConvert.SerializeObject(dataContext.Current);
        
        await distributionEventHubService.PublishAsync(new PipelineDataReceived
        {
            TenantId = adapterEtlContext.TenantId,
            PipelineRtEntityId = adapterEtlContext.PipelineRtEntityId,
            Value = s, 
            TransactionStartedDateTime = adapterEtlContext.TransactionStartedDateTime,
            ExternalReceivedDateTime = adapterEtlContext.ExternalReceivedDateTime
        });

        await next(dataContext);
    }
}