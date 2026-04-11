using Meshmakers.Octo.ConstructionKit.Contracts;

namespace Meshmakers.Octo.Communication.Contracts.MessageObjects;

/// <summary>
///     Command request sent by ToPipelineDataEventNode when AwaitResult is enabled.
///     Uses the command pattern for request/response instead of pub/sub.
/// </summary>
public record PipelineDataCommandRequest
{
    /// <summary>
    ///     The tenant identifier.
    /// </summary>
    public string TenantId { get; init; } = null!;

    /// <summary>
    ///     Gets or sets the id of the data flow.
    /// </summary>
    public OctoObjectId DataFlowRtId { get; init; }

    /// <summary>
    ///     The pipeline entity identifier.
    /// </summary>
    public RtEntityId PipelineRtEntityId { get; init; }

    /// <summary>
    ///     The serialized data value.
    /// </summary>
    public string? Value { get; init; }

    /// <summary>
    ///     The date time the transaction started.
    /// </summary>
    public DateTime TransactionStartedDateTime { get; init; }

    /// <summary>
    ///     The date time a value was externally received (e.g. at PLC).
    /// </summary>
    public DateTime? ExternalReceivedDateTime { get; init; }
}
