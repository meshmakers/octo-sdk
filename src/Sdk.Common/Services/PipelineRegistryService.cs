using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Meshmakers.Common.Shared;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration.Serializer;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Microsoft.Extensions.Logging;

namespace Meshmakers.Octo.Sdk.Common.Services;

/// <summary>
/// Implementation of the pipeline execution service
/// </summary>
public sealed class PipelineRegistryService(
    ILogger<PipelineRegistryService> logger,
    IServiceProvider serviceProvider,
    IPipelineConfigurationSerializer pipelineConfigurationSerializer)
    : IPipelineRegistryService
{
    private readonly ConcurrentDictionary<Tuple<string, RtEntityId>, PipelineRegistration> _pipelineRegistrationsById =
        new();

    private readonly ConcurrentDictionary<Tuple<string, OctoObjectId>, ICollection<PipelineRegistration>>
        _pipelineRegistrationsByDataPipelineId = new();

    /// <inheritdoc />
    public async Task RegisterPipelineAsync(string tenantId, PipelineConfigurationDto pipelineConfiguration)
    {
        logger.LogInformation(
            "Registering pipeline. TenantId: {TenantId}, PipelineRtEntityId: {PipelineRtEntityId}, DataPipelineRtId: {DataPipelineRtId}",
            tenantId, pipelineConfiguration.PipelineRtEntityId, pipelineConfiguration.DataPipelineRtId);

        // Load and check configuration
        var configurationRoot =
            await pipelineConfigurationSerializer.DeserializeAsync(pipelineConfiguration.NodeConfiguration);

        if (configurationRoot.Triggers == null)
        {
            throw PipelineExecutionException.PipelineTriggerMissing(tenantId, pipelineConfiguration.PipelineRtEntityId);
        }

        var globalConfiguration = new GlobalConfiguration(pipelineConfiguration.Configurations);

        // Register pipeline
        var pipelineRegistration = new PipelineRegistration(tenantId, pipelineConfiguration.DataPipelineRtId,
            pipelineConfiguration.PipelineRtEntityId,
            pipelineConfiguration.IsDebuggingEnabled, configurationRoot, globalConfiguration,
            new Dictionary<string, object?>());

        // Start trigger nodes
        await pipelineRegistration.StartTriggerPipelineNodesAsync(serviceProvider);

        _pipelineRegistrationsById.TryAdd(CreateByIdKey(tenantId, pipelineConfiguration.PipelineRtEntityId),
            pipelineRegistration);
        var list = _pipelineRegistrationsByDataPipelineId.GetOrAdd(
            CreateDataPipelineIdKey(tenantId, pipelineConfiguration.DataPipelineRtId),
            new List<PipelineRegistration>());
        list.Add(pipelineRegistration);
    }

    /// <inheritdoc />
    public async Task<bool> RegisterPipelinesAsync(string tenantId,
        ICollection<PipelineConfigurationDto> pipelineConfigurations,
        List<DeploymentUpdateErrorMessageDto> deploymentErrorMessages)
    {
        _pipelineRegistrationsById.Clear();
        _pipelineRegistrationsByDataPipelineId.Clear();

        logger.LogInformation(
            "Registering multiple pipelines for tenant {TenantId}. Pipeline count: {PipelineCount}",
            tenantId, pipelineConfigurations.Count);

        bool success = true;
        foreach (var pipelineConfiguration in pipelineConfigurations)
        {
            try
            {
                await RegisterPipelineAsync(tenantId, pipelineConfiguration);
            }
            catch (PipelineSerializationException e)
            {
                deploymentErrorMessages.Add(new DeploymentUpdateErrorMessageDto
                {
                    PipelineRtEntityId = pipelineConfiguration.PipelineRtEntityId,
                    DataPipelineRtId = pipelineConfiguration.DataPipelineRtId,
                    ErrorMessage = e.GetDirectAndIndirectMessages()
                });
                success = false;
            }
        }

        return success;
    }

    /// <inheritdoc />
    public async Task UnregisterPipelineAsync(string tenantId, RtEntityId pipelineRtEntityId)
    {
        if (_pipelineRegistrationsById.TryRemove(CreateByIdKey(tenantId, pipelineRtEntityId),
                out var pipelineExecutionItem))
        {
            var dataPipelineIdKey = CreateDataPipelineIdKey(tenantId, pipelineExecutionItem.DataPipelineRtId);
            if (_pipelineRegistrationsByDataPipelineId.TryGetValue(dataPipelineIdKey, out var list))
            {
                list.Remove(pipelineExecutionItem);
                if (list.Count == 0)
                {
                    _pipelineRegistrationsByDataPipelineId.TryRemove(dataPipelineIdKey, out _);
                }
            }

            await pipelineExecutionItem.StopTriggerPipelineNodesAsync();
        }
    }

    /// <inheritdoc />
    public async Task UnregisterAllPipelinesAsync(string tenantId)
    {
        foreach (var kvp in _pipelineRegistrationsByDataPipelineId.Where(x =>
                     x.Key.Item1 == tenantId.NormalizeString()))
        {
            var pipelineExecutionItems = kvp.Value.ToArray();

            foreach (var pipelineExecutionItem in pipelineExecutionItems)
            {
                await UnregisterPipelineAsync(tenantId, pipelineExecutionItem.PipelineRtEntityId);
            }
        }
    }

    /// <inheritdoc />
    public bool IsRegistered(string tenantId, RtEntityId pipelineRtEntityId)
    {
        return _pipelineRegistrationsById.ContainsKey(CreateByIdKey(tenantId, pipelineRtEntityId));
    }

    /// <inheritdoc />
#if !NETSTANDARD2_0
    public bool TryGetPipelineRegistration(string tenantId, RtEntityId pipelineRtEntityId,
        [NotNullWhen(true)] out PipelineRegistration? pipelineRegistration)
#else
    public bool TryGetPipelineRegistration(string tenantId, RtEntityId pipelineRtEntityId,
        out PipelineRegistration? pipelineRegistration)
#endif

    {
        return _pipelineRegistrationsById.TryGetValue(CreateByIdKey(tenantId, pipelineRtEntityId),
            out pipelineRegistration);
    }

    /// <summary>
    /// Create a key for the pipeline execution item
    /// </summary>
    /// <param name="tenantId"></param>
    /// <param name="pipelineRtEntityId"></param>
    /// <returns></returns>
    // ReSharper disable once MemberCanBePrivate.Global
    private static Tuple<string, RtEntityId> CreateByIdKey(string tenantId, RtEntityId pipelineRtEntityId)
    {
        return new Tuple<string, RtEntityId>(tenantId.NormalizeString(), pipelineRtEntityId);
    }

    /// <summary>
    /// Create a key for the pipeline execution item by data pipeline id
    /// </summary>
    /// <param name="tenantId"></param>
    /// <param name="dataPipelineRtId"></param>
    /// <returns></returns>
    private static Tuple<string, OctoObjectId> CreateDataPipelineIdKey(string tenantId, OctoObjectId dataPipelineRtId)
    {
        return new Tuple<string, OctoObjectId>(tenantId.NormalizeString(), dataPipelineRtId);
    }
}