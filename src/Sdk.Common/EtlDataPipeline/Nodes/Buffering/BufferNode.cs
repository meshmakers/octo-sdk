using Meshmakers.Octo.Sdk.Common.Adapters;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Buffering.EdgeBuffer;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Buffering;

/// <summary>
///     Configuration for the distribution event hub node
/// </summary>
[NodeName("BufferData", 1)]
public class BufferNodeConfiguration : TargetPathNodeConfiguration, IChildNodeConfiguration
{
    /// <summary>
    /// </summary>
    public string? BufferTime { get; set; } = "00:05:00";

    /// <summary>
    ///     An optional flag, that if set to true, will keep the data in the buffer after sending it to the distribution event
    ///     hub
    /// </summary>
    public bool KeepDataAfterSending { get; set; }

    /// <inheritdoc />
    public ICollection<NodeConfiguration>? Transformations { get; set; }
}

/// <summary>
///     Publishes the target object to the distribution event hub
/// </summary>
[NodeConfiguration(typeof(BufferNodeConfiguration))]
// ReSharper disable once ClassNeverInstantiated.Global
internal class BufferNode(
    NodeDelegate next,
    IEdgeDataBuffer buffer,
    IAdapterEtlContext context,
    IEtlDataOrchestrator orchestrator) : IPipelineNode
{
    public async Task ProcessObjectAsync(IDataContext dataContext)
    {
        // we store the data in the buffer
        await HandleLoad(dataContext);

        // we figure out if we need to reconfigure the buffer to send data
        if (!IsConfigUpToDate(dataContext))
        {
            var c = dataContext.NodeContext.GetNodeConfiguration<BufferNodeConfiguration>();

            // we need to store the configuration in the data context, so we can figure out next run if it has changed.
            context.Properties.Add(nameof(BufferNodeConfiguration), c);


            var scheduler = dataContext.GlobalServiceProvider.GetRequiredService<IBufferScheduler>();

            var timeSpan = TimeSpan.TryParse(c.BufferTime, out var ts) ? ts : TimeSpan.FromSeconds(10);

            scheduler.ScheduleOrReplace(async () =>
            {
                var adapterEtlContext = new AdapterEtlContext(context.TenantId,
                    context.DataPipelineRtId, context.PipelineExecutionId, context.PipelineRtEntityId,
                    context.TransactionStartedDateTime, context.ExternalReceivedDateTime,
                    context.Properties);

                // the user does not have to specify the buffer retrieval node
                var updatedTransforms = new List<NodeConfiguration>
                {
                    new BufferRetrievalNodeConfiguration
                    {
                        KeepDataAfterSending = c.KeepDataAfterSending,
                        TargetPath = c.TargetPath,
                    }
                };

                updatedTransforms.AddRange(c.Transformations ?? []);


                //this is the pipeline that loads the data and sends it to the buffer
                await orchestrator.ExecutePipelineAsync<IAdapterEtlContext>(
                    new PipelineConfigurationRoot { Transformations = updatedTransforms },
                    adapterEtlContext, dataContext.Debugger, new JObject());
            }, timeSpan);
        }


        await next(dataContext);
    }

    private bool IsConfigUpToDate(IDataContext dataContext)
    {
        if (!context.Properties.TryGetValue(nameof(BufferNodeConfiguration), out var c) ||
            c is not BufferNodeConfiguration config)
        {
            return false;
        }

        var currentConfig = dataContext.NodeContext.GetNodeConfiguration<BufferNodeConfiguration>();

        return JsonConvert.SerializeObject(currentConfig) == JsonConvert.SerializeObject(config);
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

        if (!TryGetTimestamp(current, dataContext, out var timestamp))
        {
            timestamp = DateTimeOffset.UtcNow;
        }

        foreach (var kvp in current)
        {
            var value = kvp.Value as JValue;
            if (value == null)
            {
                continue;
            }

            data[kvp.Key] = value.Value switch
            {
                string s => s,
                int i => i,
                double d => d,
                bool b => b,
                JArray arr => arr,
                _ => value.Value!
            };
        }

        // ensure we have a timestamp in the data
        if (!data.ContainsKey("timestamp"))
        {
            data["timestamp"] = timestamp!.Value;
        }

        var chunk = buffer.GetOrCreateOpenChunk();
        chunk.AddDataPoint(DataPoint.CreateNew(data, timestamp!.Value));

        // we have consumed the data create an empty data context for the next node;
        dataContext.Current = new JObject();
    }

    private bool TryGetTimestamp(JObject current, IDataContext dataContext, out DateTimeOffset? timeStamp)
    {
        if (Constants.TimeStampKeys.Any(current.ContainsKey))
        {
            var key = Constants.TimeStampKeys.First(current.ContainsKey);
            var value = current[key] as JValue;

            dataContext.NodeContext.Debug($"Found timestamp key: {key}");

            if (value?.Value is DateTimeOffset dt)
            {
                timeStamp = dt;
                return true;
            }
        }

        if (context.ExternalReceivedDateTime.HasValue)
        {
            dataContext.NodeContext.Debug("Using ExternalReceivedDateTime as timestamp.");
            timeStamp = context.ExternalReceivedDateTime;
            return true;
        }

        dataContext.NodeContext.Debug("No timestamp found in the data.");

        timeStamp = null;
        return false;
    }
}