using LiteDB;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Buffering.EdgeBuffer;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Buffering;

/// <summary>
///     Dummy configuration for the buffer retrieval node.
///     This is only required for the infrastructure to work.
///     Should never be set in a real pipeline configuration.
/// </summary>
[NodeName("BufferRetrievalNode", 1)]
internal record BufferRetrievalNodeConfiguration : NodeConfiguration
{
    /// <summary>
    ///     Gets or sets a value indicating whether the data should be kept after sending.
    /// </summary>
    public bool KeepDataAfterSending { get; set; }
}

[NodeConfiguration(typeof(BufferRetrievalNodeConfiguration))]
// ReSharper disable once ClassNeverInstantiated.Global
internal class BufferRetrievalNode(NodeDelegate next, IEdgeDataBuffer buffer) : IPipelineNode
{
    private readonly LiteDbBsonConverter _converter = new();
    public async Task ProcessObjectAsync(IDataContext dataContext)
    {
        buffer.TryCloseCurrentChunk(true);

        var c = dataContext.NodeContext.GetNodeConfiguration<BufferRetrievalNodeConfiguration>();

        foreach (var closedChunk in buffer.GetClosedChunks().ToList())
        {
            try
            {
                // Process each chunk in smaller chunks to prevent overly large memory usage as well as too large messages to be sent
                foreach (var chunk in Chunks(closedChunk))
                {
                    var data = _converter.MergeDictionaries(chunk);
                    
                    foreach(var key in data.Keys)
                    {
                        var token = _converter.BsonValueToJToken(data[key]);
                        dataContext.SetValueByPath(key, ValueKind.Simple, WriteMode.Overwrite, token);
                    }
                    await next(dataContext);
                }

                buffer.MarkAsSent(closedChunk);
                if (!c.KeepDataAfterSending)
                {
                    buffer.DeleteChunk(closedChunk);
                }
            }
            catch (Exception ex)
            {
                dataContext.NodeContext.Error($"Error processing closed chunk: {ex.Message}");
            }
            finally
            {
                closedChunk.Dispose();
            }
        }
    }

    private IEnumerable<Dictionary<string, BsonValue>[]> Chunks(IDisposableChunkedDataBuffer closedChunk)
    {
        return closedChunk.GetDataPoints().Select(x => x.Data)
            .Chunk(Constants.RetrievalChunkSize);
    }
}