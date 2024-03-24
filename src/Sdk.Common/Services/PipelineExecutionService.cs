using System.Collections.Concurrent;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration.Serializer;

namespace Meshmakers.Octo.Sdk.Common.Services;

/// <summary>
/// Represents an exception that occurs during pipeline execution
/// </summary>
/// <param name="TenantId">Tenant id</param>
/// <param name="PipelineRtEntityId">Pipeline runtime id</param>
/// <param name="IsDebuggingEnabled">When true, the pipeline is running in debug mode</param>
/// <param name="ConfigurationRoot">ETL pipeline configuration</param>
/// <param name="Dictionary">Dictionary shared between execution runs</param>
public record PipelineExecutionItem(
    string TenantId,
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
    /// Returns the pipeline execution items
    /// </summary>
    protected ConcurrentDictionary<Tuple<string, RtEntityId>, PipelineExecutionItem> PipelineExecutionItems { get; } = new();

    /// <inheritdoc />
    public async Task RegisterPipeline(string tenantId, PipelineConfigurationDto pipelineConfiguration)
    {
        var configurationRoot = await pipelineConfigurationSerializer.DeserializeAsync(pipelineConfiguration.PipelineDefinition);
        PipelineExecutionItems.TryAdd(CreateKey(tenantId, pipelineConfiguration.PipelineRtEntityId), new PipelineExecutionItem(tenantId, pipelineConfiguration.PipelineRtEntityId, 
            pipelineConfiguration.IsDebuggingEnabled, configurationRoot, new Dictionary<string, object?>())); 
    }

    /// <inheritdoc />
    public void UnregisterPipeline(string tenantId, RtEntityId pipelineRtEntityId)
    {
        PipelineExecutionItems.TryRemove(CreateKey(tenantId, pipelineRtEntityId), out _);
    }

    /// <inheritdoc />
    public void UpdatePipeline(string tenantId, PipelineConfigurationDto pipelineConfiguration)
    {
        if (PipelineExecutionItems.TryGetValue(CreateKey(tenantId, pipelineConfiguration), out var item))
        {
            item.ConfigurationRoot = pipelineConfigurationSerializer.DeserializeAsync(pipelineConfiguration.PipelineDefinition).Result;
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
        PipelineExecutionItems.Where(x=> x.Key.Item1 == tenantId)
            .ToList().
            ForEach(x=> PipelineExecutionItems.TryRemove(x.Key, out _));
    }

    /// <inheritdoc />
    public bool IsRegistered(string tenantId, RtEntityId pipelineRtEntityId)
    {
        return PipelineExecutionItems.ContainsKey(CreateKey(tenantId, pipelineRtEntityId));
    }

    /// <inheritdoc />
    public virtual async Task ExecuteAllPipelinesAsync(ExecutePipelineOptions executePipelineOptions)
    {
        foreach (var tuple in PipelineExecutionItems.Values)
        {
            await ExecutePipelineAsync(tuple.TenantId, tuple.PipelineRtEntityId, executePipelineOptions);
        }
    }

    /// <inheritdoc />
    public abstract Task ExecutePipelineAsync(string tenantId, RtEntityId pipelineRtEntityId, ExecutePipelineOptions executePipelineOptions, object? value = null);

    /// <summary>
    /// Create a key for the pipeline execution item
    /// </summary>
    /// <param name="tenantId"></param>
    /// <param name="pipelineConfiguration"></param>
    /// <returns></returns>
    // ReSharper disable once MemberCanBePrivate.Global
    protected static Tuple<string, RtEntityId> CreateKey(string tenantId, PipelineConfigurationDto pipelineConfiguration)
    {
        return CreateKey(tenantId, pipelineConfiguration.PipelineRtEntityId);
    }
    
    /// <summary>
    /// Create a key for the pipeline execution item
    /// </summary>
    /// <param name="tenantId"></param>
    /// <param name="pipelineRtEntityId"></param>
    /// <returns></returns>
    // ReSharper disable once MemberCanBePrivate.Global
    protected static Tuple<string, RtEntityId> CreateKey(string tenantId, RtEntityId pipelineRtEntityId)
    {
        return new Tuple<string, RtEntityId>(tenantId, pipelineRtEntityId);
    }
}