using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.Services;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Extracts;

/// <summary>
/// Configuration for polling extract node
/// </summary>
[NodeName("Polling", 1)]
// ReSharper disable once ClassNeverInstantiated.Global
public record PollingExtractNodeConfiguration : ExtractNodeConfiguration
{
    /// <summary>
    /// Defines the interval between each extraction
    /// </summary>
    public required TimeSpan Interval { get; init; }
    
    /// <summary>
    /// Defines the input data
    /// </summary>
    public JToken? Input { get; init; }
}

/// <summary>
/// Triggers the extraction of data using a polling mechanism
/// </summary>
[NodeConfiguration(typeof(PollingExtractNodeConfiguration))]
public class PollingExtractNode(IPollingService pollingService) : IExtractPipelineNode
{
    /// <inheritdoc />
    public Task RegisterAsync(IExtractNodeContext extractNodeContext)
    {
        var c = extractNodeContext.NodeContext.GetNodeConfiguration<PollingExtractNodeConfiguration>();
        pollingService.AddCallback(c.Interval, async () =>
        {
            await extractNodeContext.ExecuteAsync(c.Input);
        });
        
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UnregisterAsync(IExtractNodeContext extractNodeContext)
    {
        // TODO: Rework polling service to correctly clear callback of the registered node
        pollingService.ClearCallbacks();

        return Task.CompletedTask;
    }
}