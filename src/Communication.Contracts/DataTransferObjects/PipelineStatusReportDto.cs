using Meshmakers.Octo.ConstructionKit.Contracts;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// DTO for reporting a pipeline's live status line to the communication controller (AB#5385).
/// Named "report" because <c>PipelineStatusDto</c> is already the per-pipeline execution state
/// inside <see cref="DataFlowStatusDto"/>; this one is the adapter → controller message.
/// Sent by a trigger node after every poll — or from its failure path — so the controller can
/// write the pipeline entity's <c>StatusMessage</c> and a UI reading that attribute shows what
/// the trigger last did (mailbox polled, counts, or the error that stopped it), instead of a
/// pipeline that reads "Deployed" for days while every poll fails.
/// </summary>
public record PipelineStatusReportDto
{
    /// <summary>
    /// Pipeline the status belongs to. Must be one of the pipelines deployed to the reporting
    /// adapter; the controller rejects anything else.
    /// </summary>
    public required RtEntityId PipelineRtEntityId { get; init; }

    /// <summary>
    /// One human-readable status line. The controller truncates it server-side; senders should
    /// keep it short (a few hundred characters) and must never include credentials or message
    /// bodies.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// True when the line reports a failure (the poll threw), false for a completed poll.
    /// </summary>
    public required bool IsError { get; init; }

    /// <summary>
    /// When the status was produced on the adapter (UTC).
    /// </summary>
    public required DateTime TimestampUtc { get; init; }
}
