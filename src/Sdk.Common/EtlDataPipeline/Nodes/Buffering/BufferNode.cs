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
public record BufferNodeConfiguration : PathNodeConfiguration, IChildNodeConfiguration
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
    IEtlContext context,
    IEtlDataOrchestrator orchestrator) : IPipelineNode
{
    private readonly LiteDbBsonConverter _liteDbBsonConverter = new();

    public async Task ProcessObjectAsync(IDataContext dataContext)
    {
        var c = dataContext.NodeContext.GetNodeConfiguration<BufferNodeConfiguration>();

        // we store the data in the buffer
        HandleLoad(dataContext, c);

        // we figure out if we need to reconfigure the buffer to send data
        if (!IsConfigUpToDate(dataContext))
        {
            // we need to store the configuration in the data context, so we can figure out next run if it has changed.
            context.Properties.Add(nameof(BufferNodeConfiguration), c);

            var scheduler = dataContext.GlobalServiceProvider.GetRequiredService<IBufferScheduler>();

            var timeSpan = TimeSpan.TryParse(c.BufferTime, out var ts) ? ts : TimeSpan.FromSeconds(10);

            scheduler.ScheduleOrReplace(async () =>
            {
                // the user does not have to specify the buffer retrieval node
                var updatedTransforms = new List<NodeConfiguration>
                {
                    new BufferRetrievalNodeConfiguration
                    {
                        KeepDataAfterSending = c.KeepDataAfterSending,
                    }
                };

                updatedTransforms.AddRange(c.Transformations ?? []);

                //this is the pipeline that loads the data and sends it to the buffer
                await orchestrator.ExecutePipelineAsync(
                    new NodeDefinitionRoot { Transformations = updatedTransforms },
                    context, dataContext.Debugger, new JObject());
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

    private void HandleLoad(IDataContext dataContext, BufferNodeConfiguration c)
    {
        var data = _liteDbBsonConverter.JTokenToDictionary(dataContext.GetComplexObjectByPath<JToken>(c.Path));

        var chunk = buffer.GetOrCreateOpenChunk();
        chunk.AddDataPoint(DataPoint.CreateNew(data));

        // we have consumed the data create an empty data context for the next node;
        dataContext.Current = new JObject();
    }
}