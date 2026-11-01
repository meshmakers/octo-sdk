namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     What a failed pipeline execution says about its writes (AB#8037, rule R3 of the method consistency model).
///     Values must match the CK model enum <c>System.Communication/PipelineExecutionFailureKind</c>.
/// </summary>
/// <remarks>
///     <para>
///         The adapter classifies the exception that ended the execution with
///         <c>Meshmakers.Octo.Runtime.Contracts.UnacknowledgedWriteClassifier</c> and reports the result next to
///         <see cref="PipelineExecutionStatus.Failed" />. The communication controller stores it on the execution and a
///         pipeline method handler answers <c>METHOD_OUTCOME_UNKNOWN</c> (with a correlation id) on
///         <see cref="OutcomeUnknown" /> instead of <c>METHOD_HANDLER_FAILED</c>.
///     </para>
///     <para>
///         A structured field, never a marker in the error message: the error message is free text for people and is
///         never parsed.
///     </para>
/// </remarks>
public enum PipelineExecutionFailureKind
{
    /// <summary>
    ///     An ordinary failure: the database refused the write, or the pipeline failed for another reason. The caller of a
    ///     pipeline method gets <c>METHOD_HANDLER_FAILED</c>.
    /// </summary>
    Failed = 0,

    /// <summary>
    ///     A write was neither acknowledged nor refused (MongoDB write-concern error, <c>UnknownTransactionCommitResult</c>):
    ///     it may have taken effect. The caller of a pipeline method gets <c>METHOD_OUTCOME_UNKNOWN</c>.
    /// </summary>
    OutcomeUnknown = 1
}
