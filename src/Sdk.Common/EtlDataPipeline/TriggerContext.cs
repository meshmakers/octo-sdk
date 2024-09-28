using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.Services;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline;

/// <summary>
/// Implementation of the extract node context
/// </summary>
/// <param name="tenantId">TenantId of pipeline</param>
/// <param name="dataPipelineRtId">Runtime id of the data pipeline</param>
/// <param name="pipelineRtEntityId">Pipeline runtime id</param>
/// <param name="nodeContext">Node context of the triggering extract node</param>
public abstract class TriggerContext(string tenantId, OctoObjectId dataPipelineRtId, RtEntityId pipelineRtEntityId, INodeContext nodeContext) : ITriggerContext
{
    private readonly string _tenantId = tenantId;
    private readonly RtEntityId _pipelineRtEntityId = pipelineRtEntityId;

    /// <inheritdoc />
    public INodeContext NodeContext { get; } = nodeContext;
    
    /// <inheritdoc />
    public string TenantId { get; } = tenantId;

    /// <inheritdoc />
    public OctoObjectId DataPipelineRtId { get; } = dataPipelineRtId;

    /// <inheritdoc />
    public RtEntityId PipelineRtEntityId { get; } = pipelineRtEntityId;
    
    /// <inheritdoc />
    public async Task<object?> ExecuteAsync(ExecutePipelineOptions executePipelineOptions, object? input = null)
    {
        try
        {
            var pipelineExecutionId = await StartExecutePipelineAsync(executePipelineOptions, input);

            return await EndExecutePipelineAsync(pipelineExecutionId);
        }
        catch (Exception e)
        {
            throw PipelineExecutionException.PipelineExecutionFailed(_tenantId, _pipelineRtEntityId, e);
        }
    }

    /// <inheritdoc />
    public abstract Task<Guid> StartExecutePipelineAsync(ExecutePipelineOptions executePipelineOptions, object? value = null);

    /// <inheritdoc />
    public abstract Task<object?> EndExecutePipelineAsync(Guid pipelineExecutionId);
}