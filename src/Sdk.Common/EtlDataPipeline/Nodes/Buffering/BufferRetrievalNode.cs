using LiteDB;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Buffering.EdgeBuffer;
using Newtonsoft.Json.Linq;

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
internal class BufferRetrievalNode(NodeDelegate next, IEdgeDataBuffer<Dictionary<string, BsonValue>> buffer) : IPipelineNode
{
    private readonly LiteDbBsonConverter _converter = new();
    public async Task ProcessObjectAsync(IDataContext dataContext)
    {
        buffer.TryCloseCurrentChunk(true);

        var c = dataContext.NodeContext.GetNodeConfiguration<BufferRetrievalNodeConfiguration>();

        // Loop through each closed chunk
        foreach (var closedChunk in buffer.GetClosedChunks().ToList())
        {
            try
            {
                // Break large results into smaller pieces
                foreach (var chunk in Chunks(closedChunk))
                {
                    // Instead of merging the dictionaries, keep them as an array
                    var arrayOfObjects = new JArray();

                    foreach (var dictionaryItem in chunk)
                    {
                        var jObject = new JObject();
                        foreach (var kvp in dictionaryItem)
                        {
                            // Convert each BsonValue to a JToken without merging
                            jObject[kvp.Key] = _converter.BsonValueToJToken(kvp.Value);
                        }
                        arrayOfObjects.Add(jObject);
                    }

                    // Place this array of objects into the data context
                    // (You can store it at any path you want, below is just an example)
                    dataContext.SetValueByPath("$", ValueKind.Array, WriteMode.Overwrite, arrayOfObjects);

                    // Continue processing
                    await next(dataContext);
                }

                // Mark the chunk as sent
                buffer.MarkAsSent(closedChunk);

                // Optionally delete it if we do not keep data
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

    private IEnumerable<Dictionary<string, BsonValue>[]> Chunks(IDisposableChunkedDataBuffer<Dictionary<string, BsonValue>> closedChunk)
    {
        return closedChunk.GetDataPoints().Select(x => x.Data)
            .Chunk(Constants.RetrievalChunkSize);
    }
}