using Meshmakers.Octo.Common.DistributionEventHub.Services;
using Meshmakers.Octo.Communication.Contracts.MessageObjects;
using Meshmakers.Octo.Sdk.Common.Adapters;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
public class DistributionEventHubNode(NodeDelegate next) : IPipelineNode
{
    /// <inheritdoc />
    public async Task ProcessObjectAsync(IDataContext dataContext)
    {
        var adapterEtlContext = dataContext.PipelineServiceProvider.GetRequiredService<IAdapterEtlContext>();
        var c = dataContext.GetNodeConfiguration<DistributionEventHubNodeConfiguration>();
        var distributionEventHubService = dataContext.GlobalServiceProvider.GetRequiredService<IDistributionEventHubService>();
        dataContext.Logger.LogDebug("Executing {Node} {Description}", nameof(DistributionEventHubNode), c.Description);

        var s = JsonConvert.SerializeObject(dataContext.Current);
        
        await distributionEventHubService.PublishAsync(new UpdatedValueMessageDto
        {
            TenantId = adapterEtlContext.TenantId,
            DataPipelineRtId = adapterEtlContext.DataPipelineRtId,
            Value = s, 
            AdapterReceivedDateTime = DateTime.UtcNow
        });

        dataContext.Logger.LogDebug("Executing {Node} {Description} done - executing next", nameof(DistributionEventHubNode), c.Description);
        await next(dataContext);
    }
}