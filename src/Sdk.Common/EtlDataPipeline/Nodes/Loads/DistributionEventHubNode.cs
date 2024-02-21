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
public class DistributionEventHubNodeConfiguration : LoadNodeConfiguration;

/// <summary>
/// Publishes the target object to the distribution event hub
/// </summary>
[Node("PublishToDistributionEventHub", 1, typeof(DistributionEventHubNodeConfiguration))]
public class DistributionEventHubNode : ILoadPipelineNode
{
    /// <inheritdoc />
    public async Task ProcessObjectAsync(ILoadDataContext dataContext)
    {
        var adapterEtlContext = dataContext.PipelineServiceProvider.GetRequiredService<IAdapterEtlContext>();
        var c = dataContext.GetNodeConfiguration<DistributionEventHubNodeConfiguration>();
        var distributionEventHubService = dataContext.GlobalServiceProvider.GetRequiredService<IDistributionEventHubService>();

        var s = JsonConvert.SerializeObject(dataContext.Target);
        
        await distributionEventHubService.PublishAsync(new UpdatedValueMessageDto
        {
            TenantId = adapterEtlContext.TenantId,
            DataPipelineRtId = adapterEtlContext.DataPipelineRtId,
            Value = s, 
            AdapterReceivedDateTime = DateTime.UtcNow
        });
    }
}