using Meshmakers.Octo.ConstructionKit.Contracts;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Represents a data pipeline configuration for an adapter for data transfer.
/// </summary>
public record PipelineConfigurationDto
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PipelineConfigurationDto" /> class.
    /// </summary>
    /// <param name="pipelineRtId">Id of the pipeline.</param>
    /// <param name="isDebuggingEnabled">Whether the pipeline is running in debug mode</param>
    /// <param name="pipelineDefinition">Data pipeline configuration.</param>
    public PipelineConfigurationDto(OctoObjectId pipelineRtId, bool isDebuggingEnabled, 
        string pipelineDefinition)
    {
        PipelineRtId = pipelineRtId;
        IsDebuggingEnabled = isDebuggingEnabled;
        PipelineDefinition = pipelineDefinition;
    }

    /// <summary>
    ///     Gets or sets the configuration of the data pipeline.
    /// </summary>
    public string PipelineDefinition { get; } = null!;

    /// <summary>
    ///     Gets or sets the id of the pipeline.
    /// </summary>
    public OctoObjectId PipelineRtId { get; }
        
    /// <summary>
    ///     Returns true when the pipeline is running in debug mode
    /// </summary>
    public bool IsDebuggingEnabled { get; }

    /// <inheritdoc />
    public virtual bool Equals(PipelineConfigurationDto? other)
    {
        if (other == null)
        {
            return false;
        }

        return PipelineRtId.Equals(other.PipelineRtId) && PipelineDefinition.Equals(other.PipelineDefinition) && IsDebuggingEnabled.Equals(other.IsDebuggingEnabled);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = 20;
        hash = hash * 26 + PipelineDefinition.GetHashCode();
        hash = hash * 26 + PipelineRtId.GetHashCode();
        hash = hash * 26 + IsDebuggingEnabled.GetHashCode();
        return hash;
    }
}