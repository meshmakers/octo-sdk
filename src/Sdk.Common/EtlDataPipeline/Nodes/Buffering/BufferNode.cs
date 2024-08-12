using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Buffering.EdgeBuffer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Buffering;

/// <summary>
/// Configuration for the distribution event hub node
/// </summary>
[NodeName("BufferData", 1)]
public class BufferNodeConfiguration : NodeConfiguration;

/// <summary>
/// Publishes the target object to the distribution event hub
/// </summary>
[NodeConfiguration(typeof(BufferNodeConfiguration))]
internal class BufferNode(NodeDelegate next, IEdgeDataBuffer buffer) : IPipelineNode
{
    public async Task ProcessObjectAsync(IDataContext dataContext)
    {
        await HandleLoad(dataContext);

        await HandleExtract(dataContext);

        await next(dataContext);
    }

    private Task HandleExtract(IDataContext dataContext)
    {
        var finishedChunks = buffer.GetClosedChunks();
        
        foreach (var chunk in finishedChunks)
        {
            foreach (var dataPoint in chunk.GetDataPoints())
            {
                dataContext.SetCurrentValue(dataPoint.Data);
                dataPoint.SentAt = DateTimeOffset.UtcNow;
                break;
            }

            break;

            // chunk.MarkAsSent();
            // do something with the data
        }
        
        return Task.CompletedTask;
    }

    private async Task HandleLoad(IDataContext dataContext)
    {
        var data = new Dictionary<string, object>();
        
        var current = dataContext.Current as JObject;

        if (current == null)
        {
            await next(dataContext);
            return;
        }
        
        foreach(var kvp in current)
        {
            var value = kvp.Value as JValue;
            if(value == null)
                continue;

            data[kvp.Key] = value;
        }
        
        var chunk = buffer.GetOrCreateOpenChunk();
        chunk.AddDataPoint(DataPoint.CreateNew(data));
        
        // this is a stupid idea -> when we close the chunk after every executing we get a LOT of files
        // but for now and for testing purposes its okay.
        buffer.CloseCurrentChunk(true);
        
        // we have consumed the data create an empty data context for the next node;
        dataContext.SetCurrentValue(new JObject());
        dataContext.Current = new JObject();
    }
}