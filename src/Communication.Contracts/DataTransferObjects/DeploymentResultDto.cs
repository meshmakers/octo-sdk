using Meshmakers.Octo.ConstructionKit.Contracts;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Represents the deployment status of a pipeline.
/// </summary>
public record DeploymentResultDto
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DeploymentResultDto" /> class.
    /// </summary>
    /// <param name="pipelineRtEntityId">ID of the pipeline.</param>
    /// <param name="state">State of the deployment.</param>
    /// <param name="stateMessage">State messages.</param>
    public DeploymentResultDto( RtEntityId pipelineRtEntityId, DeploymentState state,
        string? stateMessage)
    {
        PipelineRtEntityId = pipelineRtEntityId;
        State = state;
        StateMessages = stateMessage;
    }

    /// <summary>
    /// Gets the id of the pipeline.
    /// </summary>
    public RtEntityId PipelineRtEntityId { get; }

    /// <summary>
    /// Gets the current state of the deployment.
    /// </summary>
    public DeploymentState State { get; set; }

    /// <summary>
    /// Gets or sets the data pipeline configurations.
    /// </summary>
    public string? StateMessages { get; set; }
}