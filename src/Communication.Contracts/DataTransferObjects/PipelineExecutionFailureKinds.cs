using Meshmakers.Octo.Runtime.Contracts;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Derives the <see cref="PipelineExecutionFailureKind" /> of a failed pipeline execution from the exception that ended
///     it (AB#8037). Every pipeline host (mesh adapter, SDK adapters, pool members) calls this one method, so the decision
///     is made in exactly one place: <see cref="UnacknowledgedWriteClassifier" /> of the engine, which the CK method host
///     of <c>octo-common-services</c> uses for service handlers as well.
/// </summary>
public static class PipelineExecutionFailureKinds
{
    /// <summary>
    ///     <see cref="PipelineExecutionFailureKind.OutcomeUnknown" /> when <paramref name="exception" /> (or an exception it
    ///     wraps) says that a write was not acknowledged and may have taken effect,
    ///     <see cref="PipelineExecutionFailureKind.Failed" /> otherwise (also for <c>null</c>).
    /// </summary>
    public static PipelineExecutionFailureKind Of(Exception? exception) =>
        UnacknowledgedWriteClassifier.IsOutcomeUnknown(exception)
            ? PipelineExecutionFailureKind.OutcomeUnknown
            : PipelineExecutionFailureKind.Failed;
}
