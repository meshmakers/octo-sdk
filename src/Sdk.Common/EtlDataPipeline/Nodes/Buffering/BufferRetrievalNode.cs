using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Buffering.EdgeBuffer;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Buffering;

/// <summary>
/// Dummy configuration for the buffer retrieval node.
/// This is only required for the infrastructure to work.
/// Should never be set in a real pipeline configuration.
/// </summary>
[NodeName("BufferRetrievalNode", 1)]
internal class BufferRetrievalNodeConfiguration : NodeConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether the data should be kept after sending.
    /// </summary>
    public bool? KeepDataAfterSending { get; set; }
}

[NodeConfiguration(typeof(BufferRetrievalNodeConfiguration))]
internal class BufferRetrievalNode(NodeDelegate next, IEdgeDataBuffer buffer) : IPipelineNode
{
    public async Task ProcessObjectAsync(IDataContext dataContext)
    {
        buffer.TryCloseCurrentChunk(true);

        var config = dataContext.GetNodeConfiguration<BufferRetrievalNodeConfiguration>();
        var shouldDelete = config.KeepDataAfterSending.GetValueOrDefault(false);

        foreach (var closedChunk in buffer.GetClosedChunks().ToList())
        {
            try
            {
                foreach (var dataPoint in closedChunk.GetDataPoints())
                {
                    dataContext.SetCurrentValue(dataPoint.Data);
                    await next(dataContext);
                }

                buffer.MarkAsSent(closedChunk);
                if (shouldDelete)
                {
                    buffer.DeleteChunk(closedChunk);
                }
            }
            catch (Exception ex)
            {
                dataContext.Logger.Error(dataContext.NodeStack.Peek(), $"Error processing closed chunk: {ex.Message}");
            }
        }
    }
}