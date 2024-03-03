using System.Collections.Concurrent;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration.Serializer;

namespace Meshmakers.Octo.Sdk.Common.Services;

/// <summary>
/// Represents an exception that occurs during pipeline execution
/// </summary>
/// <param name="TenantId"></param>
/// <param name="PipelineRtId"></param>
/// <param name="PipelineName"></param>
/// <param name="ConfigurationRoot"></param>
/// <param name="Dictionary"></param>
public record PipelineExecutionItem(
    string TenantId,
    OctoObjectId PipelineRtId,
    string PipelineName,
    PipelineConfigurationRoot ConfigurationRoot,
    Dictionary<string, object?> Dictionary)
{
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
    /// Returns the pipeline execution items
    /// </summary>
    protected ConcurrentDictionary<Tuple<string, OctoObjectId>, PipelineExecutionItem> PipelineExecutionItems { get; } = new();

    /// <inheritdoc />
    public async Task RegisterPipeline(string tenantId, DataPipelineConfigurationDto pipelineConfiguration)
    {
        var configurationRoot = await pipelineConfigurationSerializer.DeserializeAsync(pipelineConfiguration.DataPipelineConfiguration);
        PipelineExecutionItems.TryAdd(CreateKey(tenantId, pipelineConfiguration.DataPipelineRtId), new PipelineExecutionItem(tenantId, pipelineConfiguration.DataPipelineRtId, 
            pipelineConfiguration.Name, configurationRoot, new Dictionary<string, object?>())); 
    }

    /// <inheritdoc />
    public void UnregisterPipeline(string tenantId, OctoObjectId pipelineRtId)
    {
        PipelineExecutionItems.TryRemove(CreateKey(tenantId, pipelineRtId), out _);
    }

    /// <inheritdoc />
    public void UpdatePipeline(string tenantId, DataPipelineConfigurationDto pipelineConfiguration)
    {
        if (PipelineExecutionItems.TryGetValue(CreateKey(tenantId, pipelineConfiguration), out var item))
        {
            item.ConfigurationRoot = pipelineConfigurationSerializer.DeserializeAsync(pipelineConfiguration.DataPipelineConfiguration).Result;
        }
        else
        {
            throw PipelineExecutionException.PipelineNotFound(tenantId, pipelineConfiguration.DataPipelineRtId);
        }
    }

    /// <inheritdoc />
    public void UnregisterAllPipelines(string tenantId)
    {
        PipelineExecutionItems.Where(x=> x.Key.Item1 == tenantId)
            .ToList().
            ForEach(x=> PipelineExecutionItems.TryRemove(x.Key, out _));
    }

    /// <inheritdoc />
    public bool IsRegistered(string tenantId, OctoObjectId pipelineRtId)
    {
        return PipelineExecutionItems.ContainsKey(CreateKey(tenantId, pipelineRtId));
    }

    /// <inheritdoc />
    public virtual async Task ExecuteAllPipelinesAsync(ExecutePipelineOptions executePipelineOptions)
    {
        foreach (var tuple in PipelineExecutionItems.Values)
        {
            await ExecutePipelineAsync(tuple.TenantId, tuple.PipelineRtId, executePipelineOptions);
        }
    }

    /// <inheritdoc />
    public abstract Task ExecutePipelineAsync(string tenantId, OctoObjectId pipelineRtId, ExecutePipelineOptions executePipelineOptions, object? value = null);

    /// <summary>
    /// Create a key for the pipeline execution item
    /// </summary>
    /// <param name="tenantId"></param>
    /// <param name="pipelineConfiguration"></param>
    /// <returns></returns>
    // ReSharper disable once MemberCanBePrivate.Global
    protected static Tuple<string, OctoObjectId> CreateKey(string tenantId, DataPipelineConfigurationDto pipelineConfiguration)
    {
        return CreateKey(tenantId, pipelineConfiguration.DataPipelineRtId);
    }
    
    /// <summary>
    /// Create a key for the pipeline execution item
    /// </summary>
    /// <param name="tenantId"></param>
    /// <param name="pipelineRtId"></param>
    /// <returns></returns>
    // ReSharper disable once MemberCanBePrivate.Global
    protected static Tuple<string, OctoObjectId> CreateKey(string tenantId, OctoObjectId pipelineRtId)
    {
        return new Tuple<string, OctoObjectId>(tenantId, pipelineRtId);
    }
}