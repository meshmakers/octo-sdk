namespace Meshmakers.Octo.Communication.Contracts.MessageObjects;

/// <summary>
///     Response returned by FromPipelineDataEventNode after the target pipeline completes.
/// </summary>
public record PipelineDataCommandResponse
{
    /// <summary>
    ///     Whether the target pipeline executed successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    ///     The serialized result from the target pipeline's data context (JSON).
    /// </summary>
    public string? Result { get; init; }

    /// <summary>
    ///     Error message if the target pipeline failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
