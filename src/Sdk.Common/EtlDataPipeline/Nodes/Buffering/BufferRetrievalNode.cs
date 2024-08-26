using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Buffering.EdgeBuffer;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Buffering;

[NodeName("BufferRetrievalNode", 1)]
internal class BufferRetrievalNodeConfiguration : NodeConfiguration
{
}

[NodeConfiguration(typeof(BufferRetrievalNodeConfiguration))]
internal class BufferRetrievalNode(NodeDelegate next, IEdgeDataBuffer buffer) : IPipelineNode
{
    public async Task ProcessObjectAsync(IDataContext dataContext)
    {
        buffer.TryCloseCurrentChunk(true);

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
                buffer.DeleteChunk(closedChunk);
            }
            catch (Exception ex)
            {
                dataContext.Logger.Error(dataContext.NodeStack.Peek(), $"Error processing closed chunk: {ex.Message}");
            }
        }
    }
}