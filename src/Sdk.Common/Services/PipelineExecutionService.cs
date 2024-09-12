using System.Collections.Concurrent;
using Meshmakers.Common.Shared;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration.Serializer;

namespace Meshmakers.Octo.Sdk.Common.Services;

/// <summary>
/// Represents an exception that occurs during pipeline execution
/// </summary>
/// <param name="TenantId">Tenant id</param>
/// <param name="DataPipelineRtId">Data pipeline runtime id</param>
/// <param name="PipelineRtEntityId">Pipeline id</param>
/// <param name="IsDebuggingEnabled">When true, the pipeline is running in debug mode</param>
/// <param name="ConfigurationRoot">ETL pipeline configuration</param>
/// <param name="Dictionary">Dictionary shared between execution runs</param>
public record PipelineExecutionItem(
    string TenantId,
    OctoObjectId DataPipelineRtId,
    RtEntityId PipelineRtEntityId,
    bool IsDebuggingEnabled,
    PipelineConfigurationRoot ConfigurationRoot,
    Dictionary<string, object?> Dictionary)
{
    /// <summary>
    /// Returns true if the pipeline is running in debug mode
    /// </summary>
    public bool IsDebuggingEnabled { get; set; } = IsDebuggingEnabled;

    /// <summary>
    /// Returns the configuration root
    /// </summary>
    public PipelineConfigurationRoot ConfigurationRoot { get; set; } = ConfigurationRoot;
}

/// <summary>
/// Implementation of the pipeline execution service
/// </summary>
public abstract class PipelineExecutionService(IPipelineConfigurationSerializer pipelineConfigurationSerializer)
    : IPipelineExecutionService
{
    /// <summary>
    /// Returns the pipeline execution items by id
    /// </summary>
    protected ConcurrentDictionary<Tuple<string, RtEntityId>, PipelineExecutionItem>
        PipelineExecutionItemsById { get; } = new();

    /// <summary>
    /// Returns the pipeline execution items by data pipeline
    /// </summary>
    protected ConcurrentDictionary<Tuple<string, OctoObjectId>, ICollection<PipelineExecutionItem>>
        PipelineExecutionItemsByDataPipelineId { get; } = new();

    /// <inheritdoc />
    public async Task RegisterPipeline(string tenantId, PipelineConfigurationDto pipelineConfiguration)
    {
        var configurationRoot =
            await pipelineConfigurationSerializer.DeserializeAsync(pipelineConfiguration.PipelineDefinition);
        var pipelineExecutionItem = new PipelineExecutionItem(tenantId, pipelineConfiguration.DataPipelineRtId,
            pipelineConfiguration.PipelineRtEntityId,
            pipelineConfiguration.IsDebuggingEnabled, configurationRoot, new Dictionary<string, object?>());

        PipelineExecutionItemsById.TryAdd(CreateByIdKey(tenantId, pipelineConfiguration.PipelineRtEntityId),
            pipelineExecutionItem);
        var list = PipelineExecutionItemsByDataPipelineId.GetOrAdd(
            CreateDataPipelineIdKey(tenantId, pipelineConfiguration.DataPipelineRtId), new List<PipelineExecutionItem>());
        list.Add(pipelineExecutionItem);
    }

    /// <inheritdoc />
    public void UnregisterPipeline(string tenantId, RtEntityId pipelineRtEntityId)
    {
        if (PipelineExecutionItemsById.TryRemove(CreateByIdKey(tenantId, pipelineRtEntityId),
                out var pipelineExecutionItem))
        {
            var dataPipelineIdKey = CreateDataPipelineIdKey(tenantId, pipelineExecutionItem.DataPipelineRtId);
            if (PipelineExecutionItemsByDataPipelineId.TryGetValue(dataPipelineIdKey, out var list))
            {
                list.Remove(pipelineExecutionItem);
                if (list.Count == 0)
                {
                    PipelineExecutionItemsByDataPipelineId.TryRemove(dataPipelineIdKey, out _);
                }
            }
        }
    }

    /// <inheritdoc />
    public void UpdatePipeline(string tenantId, PipelineConfigurationDto pipelineConfiguration)
    {
        if (PipelineExecutionItemsById.TryGetValue(CreateByIdKey(tenantId, pipelineConfiguration), out var item))
        {
            item.ConfigurationRoot = pipelineConfigurationSerializer
                .DeserializeAsync(pipelineConfiguration.PipelineDefinition).Result;
            item.IsDebuggingEnabled = pipelineConfiguration.IsDebuggingEnabled;
        }
        else
        {
            throw PipelineExecutionException.PipelineNotFound(tenantId, pipelineConfiguration.PipelineRtEntityId);
        }
    }

    /// <inheritdoc />
    public void UnregisterAllPipelines(string tenantId)
    {
        foreach (var kvp in PipelineExecutionItemsByDataPipelineId.Where(x => x.Key.Item1 == tenantId.NormalizeString()))
        {
            var dataPipelineRtId = kvp.Key.Item2;
            var pipelineExecutionItems = kvp.Value;
            
            foreach (var pipelineExecutionItem in pipelineExecutionItems)
            {
                PipelineExecutionItemsById.TryRemove(CreateByIdKey(tenantId, pipelineExecutionItem.PipelineRtEntityId), out _);
            }

            PipelineExecutionItemsByDataPipelineId.TryRemove(CreateDataPipelineIdKey(tenantId, dataPipelineRtId), out _);
        }
    }

    /// <inheritdoc />
    public bool IsRegistered(string tenantId, RtEntityId pipelineRtEntityId)
    {
        return PipelineExecutionItemsById.ContainsKey(CreateByIdKey(tenantId, pipelineRtEntityId));
    }

    /// <inheritdoc />
    public virtual async Task ExecuteAllPipelinesAsync(ExecutePipelineOptions executePipelineOptions, object? value = null)
    {
        foreach (var tuple in PipelineExecutionItemsById.Values)
        {
            await ExecutePipelineAsync(tuple.TenantId, tuple.PipelineRtEntityId, executePipelineOptions, value);
        }
    }

    /// <inheritdoc />
    public abstract Task<object?> ExecutePipelineAsync(string tenantId, RtEntityId pipelineRtEntityId,
        ExecutePipelineOptions executePipelineOptions, object? value = null);

    /// <summary>
    /// Create a key for the pipeline execution item
    /// </summary>
    /// <param name="tenantId"></param>
    /// <param name="pipelineConfiguration"></param>
    /// <returns></returns>
    // ReSharper disable once MemberCanBePrivate.Global
    protected static Tuple<string, RtEntityId> CreateByIdKey(string tenantId,
        PipelineConfigurationDto pipelineConfiguration)
    {
        return CreateByIdKey(tenantId, pipelineConfiguration.PipelineRtEntityId);
    }

    /// <summary>
    /// Create a key for the pipeline execution item
    /// </summary>
    /// <param name="tenantId"></param>
    /// <param name="pipelineRtEntityId"></param>
    /// <returns></returns>
    // ReSharper disable once MemberCanBePrivate.Global
    protected static Tuple<string, RtEntityId> CreateByIdKey(string tenantId, RtEntityId pipelineRtEntityId)
    {
        return new Tuple<string, RtEntityId>(tenantId.NormalizeString(), pipelineRtEntityId);
    }

    /// <summary>
    /// Create a key for the pipeline execution item by data pipeline id
    /// </summary>
    /// <param name="tenantId"></param>
    /// <param name="dataPipelineRtId"></param>
    /// <returns></returns>
    protected static Tuple<string, OctoObjectId> CreateDataPipelineIdKey(string tenantId, OctoObjectId dataPipelineRtId)
    {
        return new Tuple<string, OctoObjectId>(tenantId.NormalizeString(), dataPipelineRtId);
    }
}