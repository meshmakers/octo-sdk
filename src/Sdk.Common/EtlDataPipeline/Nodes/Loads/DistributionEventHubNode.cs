using Meshmakers.Octo.Common.DistributionEventHub.Services;
using Meshmakers.Octo.Communication.Contracts.MessageObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Loads;

/// <summary>
/// Configuration for the distribution event hub node
/// </summary>
public class DistributionEventHubNodeConfiguration : LoadNodeConfiguration
{
    /// <summary>
    /// The tenant id
    /// </summary>
    public string TenantId { get; set; } = null!;
    
    /// <summary>
    /// The adapter object identifier.
    /// </summary>
    public OctoObjectId AdapterRtId { get; set; }
    
    /// <summary>
    /// The data pipeline object identifier.
    /// </summary>
    public OctoObjectId DataPipelineRtId { get; set; }
}

/// <summary>
/// Publishes the target object to the distribution event hub
/// </summary>
[Node("PublishDistributionEventHub", 1, typeof(ByPathNodeConfiguration))]
public class DistributionEventHubNode(IDistributionEventHubService distributionEventHubService) : ILoadPipelineNode
{
    /// <inheritdoc />
    public async Task ProcessObjectAsync(ILoadDataContext dataContext)
    {
        var c = dataContext.GetNodeConfiguration<DistributionEventHubNodeConfiguration>();
        
        await distributionEventHubService.PublishAsync(new UpdatedValueMessageDto
        {
            TenantId = c.TenantId,
            AdapterRtId = c.AdapterRtId,
            DataPipelineRtId = c.DataPipelineRtId,
            Value = dataContext.Target, 
            AdapterReceivedDateTime = DateTime.UtcNow
        });
    }
}