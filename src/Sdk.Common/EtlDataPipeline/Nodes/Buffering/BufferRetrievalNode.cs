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
        try
        {
            buffer.TryCloseCurrentChunk(true);
        }
        catch (EdgeDataBufferException)
        {
            //it is fine we don't have a chunk to close.
        }

        foreach (var closedChunk in buffer.GetClosedChunks().ToList())
        {
            try
            {
                foreach (var dataPoint in closedChunk.GetDataPoints())
                {
                    dataContext.SetCurrentValue(dataPoint);
                    await next(dataContext);
                }

                buffer.DeleteChunk(closedChunk);
            }
            catch (Exception ex)
            {
                dataContext.Logger.Error(dataContext.NodeStack.Peek(), $"Error processing closed chunk: {ex.Message}");
            }
        }
    }
}