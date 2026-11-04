using System.Text.Json.Serialization;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Represents the data of a pipeline execution
/// </summary>
public record PipelineExecutionDataDto
{
    /// <summary>
    /// ID of the pipeline execution
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Execution start date and time
    /// </summary>
    public required DateTime DateTime { get; init; }

    /// <summary>
    /// Execution status. Null if status is unknown (e.g. from in-memory debug cache).
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PipelineExecutionStatus? Status { get; init; }

    /// <summary>
    /// Duration of the execution in milliseconds. Null if still running or unknown.
    /// </summary>
    public long? DurationMs { get; init; }

    /// <summary>
    /// Error message if the execution failed. Null otherwise.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// What a failed execution says about its writes (AB#8156): <see cref="PipelineExecutionFailureKind.OutcomeUnknown" />
    /// when a write was neither acknowledged nor refused and may have taken effect, <see cref="PipelineExecutionFailureKind.Failed" />
    /// for an ordinary failure. Null for an execution that did not fail, for a failure whose adapter reported no kind
    /// (adapter built before AB#8037), and for executions that were interrupted or buffered offline - those carry no kind.
    /// Serialized as the enum name (<c>"OutcomeUnknown"</c>); absent from the JSON of older controllers.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PipelineExecutionFailureKind? FailureKind { get; init; }

    /// <summary>
    /// Whether debug point data is available for this execution.
    /// </summary>
    public bool HasDebugData { get; init; }
}
