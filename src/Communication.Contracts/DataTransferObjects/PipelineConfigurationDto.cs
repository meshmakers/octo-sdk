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
    /// <param name="dataFlowRtId">ID of the data flow.</param>
    /// <param name="pipelineRtEntityId">ID of the pipeline.</param>
    /// <param name="isDebuggingEnabled">Whether the pipeline is running in debug mode</param>
    /// <param name="nodeConfiguration">Data pipeline configuration.</param>
    /// <param name="configurations"></param>
    public PipelineConfigurationDto(OctoObjectId dataFlowRtId, RtEntityId pipelineRtEntityId, bool isDebuggingEnabled,
        string nodeConfiguration, IEnumerable<ConfigurationDto> configurations)
    {
        DataFlowRtId = dataFlowRtId;
        PipelineRtEntityId = pipelineRtEntityId;
        IsDebuggingEnabled = isDebuggingEnabled;
        NodeConfiguration = nodeConfiguration;
        Configurations = configurations;
    }

    /// <summary>
    ///     Gets or sets the node configuration of the data pipeline.
    /// </summary>
    public string NodeConfiguration { get; }
    
    /// <summary>
    ///     Gets or sets the configurations of the data pipeline.
    /// </summary>
    public IEnumerable<ConfigurationDto> Configurations { get; } 

    /// <summary>
    ///     Gets or sets the id of the data flow.
    /// </summary>
    public OctoObjectId DataFlowRtId { get; }

    /// <summary>
    ///     Gets or sets the id of the pipeline.
    /// </summary>
    public RtEntityId PipelineRtEntityId { get; }
        
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

        var configs = Configurations
            .All(x => other.Configurations.Any(y => y.Equals(x)));
        
        return DataFlowRtId.Equals(other.DataFlowRtId) &&
               PipelineRtEntityId.Equals(other.PipelineRtEntityId) &&
               NodeConfiguration.Equals(other.NodeConfiguration) && 
               configs &&
               Configurations.Count().Equals(other.Configurations.Count()) &&
               IsDebuggingEnabled.Equals(other.IsDebuggingEnabled);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = 20;
        hash = hash * 26 + DataFlowRtId.GetHashCode();
        hash = hash * 26 + NodeConfiguration.GetHashCode();
        hash = hash * 26 + PipelineRtEntityId.GetHashCode();
        hash = hash * 26 + Configurations.GetHashCode();
        hash = hash * 26 + IsDebuggingEnabled.GetHashCode();
        return hash;
    }
}