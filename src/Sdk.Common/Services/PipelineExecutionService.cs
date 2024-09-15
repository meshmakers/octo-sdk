using System.Collections.Concurrent;
using Meshmakers.Common.Shared;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration.Serializer;

namespace Meshmakers.Octo.Sdk.Common.Services;

/// <summary>
/// Defines the pipeline execution service
/// </summary>
public enum PipelineExecutionStatus
{
    /// <summary>
    /// The pipeline execution is running
    /// </summary>
    Running,

    /// <summary>
    /// The pipeline execution has completed
    /// </summary>
    Completed,

    /// <summary>
    /// The pipeline execution has failed
    /// </summary>
    Failed
}

/// <summary>
/// Represents the lifetime of a pipeline execution
/// </summary>
/// <param name="PipelineExecutionId">The pipeline execution id</param>
/// <param name="StartedDateTime">The date and time the pipeline execution started</param>
/// <param name="ExecutePipelineTask">The task that executes the pipeline</param>
/// <param name="Properties">Properties to store state between start- and end pipeline</param>
// ReSharper disable once NotAccessedPositionalProperty.Global
public record PipelineExecution(Guid PipelineExecutionId, DateTime StartedDateTime, Task<object?> ExecutePipelineTask,
    // ReSharper disable once NotAccessedPositionalProperty.Global
    Dictionary<string, object?> Properties);

/// <summary>
/// Represents an exception that occurs during pipeline execution
/// </summary>
/// <param name="TenantId">Tenant id</param>
/// <param name="DataPipelineRtId">Data pipeline runtime id</param>
/// <param name="PipelineRtEntityId">Pipeline id</param>
/// <param name="IsDebuggingEnabled">When true, the pipeline is running in debug mode</param>
/// <param name="ConfigurationRoot">ETL pipeline configuration</param>
/// <param name="Dictionary">Dictionary shared between execution runs</param>
public record PipelineRegistration(
    string TenantId,
    OctoObjectId DataPipelineRtId,
    RtEntityId PipelineRtEntityId,
    bool IsDebuggingEnabled,
    PipelineConfigurationRoot ConfigurationRoot,
    Dictionary<string, object?> Dictionary)
{
    /// <summary>
    /// A list of pipeline executions
    /// </summary>
    private readonly ConcurrentDictionary<Guid, PipelineExecution> _pipelineExecutions = new();
    
    /// <summary>
    /// Returns true if the pipeline is running in debug mode
    /// </summary>
    public bool IsDebuggingEnabled { get; set; } = IsDebuggingEnabled;

    /// <summary>
    /// Returns the configuration root
    /// </summary>
    public PipelineConfigurationRoot ConfigurationRoot { get; set; } = ConfigurationRoot;
    
    /// <summary>
    /// Get the execution property value
    /// </summary>
    /// <param name="pipelineExecutionId">ID of the pipeline execution</param>
    /// <param name="key">Key of the property</param>
    /// <typeparam name="T">Type of the property</typeparam>
    /// <returns>Value of the property</returns>
    // ReSharper disable once UnusedMember.Global
    public T? GetExecutionPropertyValue<T>(Guid pipelineExecutionId, string key)
    {
        if (_pipelineExecutions.TryGetValue(pipelineExecutionId, out var pipelineExecution))
        {
            var o = pipelineExecution.Properties[key];
            if (o == null)
            {
                return default;
            }
            return o is T o1 ? o1 : default;
        }

        return default;
    }

    /// <summary>
    /// Registers a pipeline execution
    /// </summary>
    /// <param name="pipelineExecutionId">ID of the pipeline execution</param>
    /// <param name="startedDateTime">Date and time the pipeline execution started</param>
    /// <param name="executePipelineTask"></param>
    // ReSharper disable once UnusedMethodReturnValue.Global
    public PipelineExecution RegisterExecution(Guid pipelineExecutionId, DateTime startedDateTime, Task<object?> executePipelineTask)
    {
        var pipelineExecution = new PipelineExecution(pipelineExecutionId, startedDateTime,
            executePipelineTask, new Dictionary<string, object?>());
        _pipelineExecutions.TryAdd(pipelineExecutionId, pipelineExecution);
        
        // Only allow 10 executions to be stored
        if (_pipelineExecutions.Count > 10)
        {
            var oldest = _pipelineExecutions.OrderBy(x => x.Value.StartedDateTime).First();
            _pipelineExecutions.TryRemove(oldest.Key, out _);
        }

        return pipelineExecution;
    }

    /// <summary>
    /// Wait for the pipeline execution to complete and returns the result
    /// </summary>
    /// <param name="pipelineExecutionId">The pipeline execution id</param>
    /// <returns>The result of the pipeline execution</returns>
    public async Task<object?> UnregisterExecutionAsync(Guid pipelineExecutionId)
    {
        if (_pipelineExecutions.TryGetValue(pipelineExecutionId, out var pipelineExecution))
        {
            var result = await pipelineExecution.ExecutePipelineTask;
            _pipelineExecutions.TryRemove(pipelineExecutionId, out _);
            return result;
        }
        
        throw PipelineExecutionException.PipelineExecutionNotFound(TenantId, PipelineRtEntityId, pipelineExecutionId);
    }
    
    /// <summary>
    /// Get the status of the pipeline execution
    /// </summary>
    /// <param name="pipelineExecutionId">The pipeline execution id</param>
    /// <returns></returns>
    public PipelineExecutionStatus GetPipelineExecutionStatus(Guid pipelineExecutionId)
    {
        if (_pipelineExecutions.TryGetValue(pipelineExecutionId, out var pipelineExecution))
        {
            if (pipelineExecution.ExecutePipelineTask.IsFaulted)
            {
                return PipelineExecutionStatus.Failed;
            }
            
            if (pipelineExecution.ExecutePipelineTask.IsCompleted)
            {
                return PipelineExecutionStatus.Completed;
            }

            return PipelineExecutionStatus.Running;
        }

        return PipelineExecutionStatus.Failed;
    }
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
    protected ConcurrentDictionary<Tuple<string, RtEntityId>, PipelineRegistration>
        PipelineRegistrationsById { get; } = new();

    /// <summary>
    /// Returns the pipeline execution items by data pipeline
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    protected ConcurrentDictionary<Tuple<string, OctoObjectId>, ICollection<PipelineRegistration>>
        PipelineRegistrationsByDataPipelineId { get; } = new();

    /// <inheritdoc />
    public async Task RegisterPipeline(string tenantId, PipelineConfigurationDto pipelineConfiguration)
    {
        var configurationRoot =
            await pipelineConfigurationSerializer.DeserializeAsync(pipelineConfiguration.PipelineDefinition);
        var pipelineRegistration = new PipelineRegistration(tenantId, pipelineConfiguration.DataPipelineRtId,
            pipelineConfiguration.PipelineRtEntityId,
            pipelineConfiguration.IsDebuggingEnabled, configurationRoot, new Dictionary<string, object?>());

        PipelineRegistrationsById.TryAdd(CreateByIdKey(tenantId, pipelineConfiguration.PipelineRtEntityId),
            pipelineRegistration);
        var list = PipelineRegistrationsByDataPipelineId.GetOrAdd(
            CreateDataPipelineIdKey(tenantId, pipelineConfiguration.DataPipelineRtId), new List<PipelineRegistration>());
        list.Add(pipelineRegistration);
    }

    /// <inheritdoc />
    public void UnregisterPipeline(string tenantId, RtEntityId pipelineRtEntityId)
    {
        if (PipelineRegistrationsById.TryRemove(CreateByIdKey(tenantId, pipelineRtEntityId),
                out var pipelineExecutionItem))
        {
            var dataPipelineIdKey = CreateDataPipelineIdKey(tenantId, pipelineExecutionItem.DataPipelineRtId);
            if (PipelineRegistrationsByDataPipelineId.TryGetValue(dataPipelineIdKey, out var list))
            {
                list.Remove(pipelineExecutionItem);
                if (list.Count == 0)
                {
                    PipelineRegistrationsByDataPipelineId.TryRemove(dataPipelineIdKey, out _);
                }
            }
        }
    }

    /// <inheritdoc />
    public void UpdatePipeline(string tenantId, PipelineConfigurationDto pipelineConfiguration)
    {
        if (PipelineRegistrationsById.TryGetValue(CreateByIdKey(tenantId, pipelineConfiguration), out var item))
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
        foreach (var kvp in PipelineRegistrationsByDataPipelineId.Where(x => x.Key.Item1 == tenantId.NormalizeString()))
        {
            var dataPipelineRtId = kvp.Key.Item2;
            var pipelineExecutionItems = kvp.Value;
            
            foreach (var pipelineExecutionItem in pipelineExecutionItems)
            {
                PipelineRegistrationsById.TryRemove(CreateByIdKey(tenantId, pipelineExecutionItem.PipelineRtEntityId), out _);
            }

            PipelineRegistrationsByDataPipelineId.TryRemove(CreateDataPipelineIdKey(tenantId, dataPipelineRtId), out _);
        }
    }

    /// <inheritdoc />
    public bool IsRegistered(string tenantId, RtEntityId pipelineRtEntityId)
    {
        return PipelineRegistrationsById.ContainsKey(CreateByIdKey(tenantId, pipelineRtEntityId));
    }

    /// <inheritdoc />
    public virtual async Task ExecuteAllPipelinesAsync(ExecutePipelineOptions executePipelineOptions, object? value = null)
    {
        foreach (var tuple in PipelineRegistrationsById.Values)
        {
            await ExecutePipelineAsync(tuple.TenantId, tuple.PipelineRtEntityId, executePipelineOptions, value);
        }
    }

    /// <inheritdoc />
    public async Task<object?> ExecutePipelineAsync(string tenantId, RtEntityId pipelineRtEntityId,
        ExecutePipelineOptions executePipelineOptions, object? value = null)
    {
        try
        {
            var pipelineExecutionId = await StartExecutePipelineAsync(tenantId, pipelineRtEntityId, executePipelineOptions, value);

            return await EndExecutePipelineAsync(tenantId, pipelineRtEntityId, pipelineExecutionId);
        }
        catch (Exception e)
        {
            throw PipelineExecutionException.PipelineExecutionFailed(tenantId, pipelineRtEntityId, e);
        }
    }

    /// <inheritdoc />
    public abstract Task<Guid> StartExecutePipelineAsync(string tenantId, RtEntityId pipelineRtEntityId,
        ExecutePipelineOptions executePipelineOptions, object? value = null);

    /// <inheritdoc />
    public abstract Task<object?> EndExecutePipelineAsync(string tenantId, RtEntityId pipelineRtEntityId,
        Guid pipelineExecutionId);
  

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