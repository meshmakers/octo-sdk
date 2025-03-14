using Meshmakers.Octo.ConstructionKit.Contracts;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Defines an deployment error message
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class DeploymentUpdateErrorMessageDto
{
    /// <summary>
    ///     Gets or sets the id of the data pipeline.
    /// </summary>
    public OctoObjectId? DataPipelineRtId { get; init; }

    /// <summary>
    ///     Gets or sets the id of the pipeline.
    /// </summary>
    public RtEntityId? PipelineRtEntityId { get; init; }

    /// <summary>
    ///     The error message
    /// </summary>
    public required string ErrorMessage { get; init; }
}