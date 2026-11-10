using System.Text.Json;

namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants.Methods;

/// <summary>
///     The state of an operation of an asynchronous CK method (AB#8446; GraphQL enum <c>CkMethodOperationState</c>).
/// </summary>
public enum CkOperationState
{
    /// <summary>A state this client does not know (a newer service); treated as open.</summary>
    Unknown = 0,

    /// <summary>Accepted, the work has not started yet (open).</summary>
    Accepted,

    /// <summary>The work runs (open).</summary>
    Running,

    /// <summary>The work ended successfully (final); the result is in <see cref="CkOperation.Result" />.</summary>
    Succeeded,

    /// <summary>The work failed (final); the error is in <see cref="CkOperation.Error" />.</summary>
    Failed
}

/// <summary>
///     The last progress report of an operation (AB#8446): an optional percentage and a language-neutral stage code with
///     parameters; no message text, the client renders it in its own language.
/// </summary>
/// <param name="Percent">Done in percent (0 to 100); <c>null</c> when the handler cannot tell</param>
/// <param name="StageCode">Stable stage code (UPPER_SNAKE_CASE), e.g. <c>DUMPING_DATABASE</c>; <c>null</c> when not reported</param>
/// <param name="Parameters">Parameters of the stage (e.g. <c>jobId</c>); empty when there are none</param>
public sealed record CkOperationProgress(int? Percent, string? StageCode, IReadOnlyDictionary<string, string> Parameters);

/// <summary>
///     One operation of an asynchronous CK method as <c>runtime.operations</c> returns it (AB#8446).
/// </summary>
public sealed class CkOperation
{
    /// <summary>
    ///     Creates the operation.
    /// </summary>
    /// <param name="operationId">The opaque id of the operation</param>
    /// <param name="methodId">The qualified, model-version-less id of the method</param>
    /// <param name="invocationId">The id of the invocation that started it</param>
    /// <param name="state">The current state</param>
    /// <param name="stateName">The state as the service sent it (e.g. <c>RUNNING</c>)</param>
    /// <param name="progress">The last progress report; <c>null</c> when none was reported</param>
    /// <param name="result">The result of a succeeded operation (JSON); <c>null</c> when there is none</param>
    /// <param name="error">The error of a failed operation; <c>null</c> otherwise</param>
    /// <param name="acceptedAtUtc">The time (UTC) the operation was accepted</param>
    /// <param name="startedAtUtc">The time (UTC) the handler started the work</param>
    /// <param name="completedAtUtc">The time (UTC) the operation became final</param>
    /// <param name="expiresAtUtc">The time (UTC) after which the operation is forgotten</param>
    public CkOperation(string operationId, string methodId, string? invocationId, CkOperationState state,
        string stateName, CkOperationProgress? progress, JsonElement? result, CkMethodError? error,
        DateTime? acceptedAtUtc, DateTime? startedAtUtc, DateTime? completedAtUtc, DateTime? expiresAtUtc)
    {
        OperationId = operationId;
        MethodId = methodId;
        InvocationId = invocationId;
        State = state;
        StateName = stateName;
        Progress = progress;
        Result = result;
        Error = error;
        AcceptedAtUtc = acceptedAtUtc;
        StartedAtUtc = startedAtUtc;
        CompletedAtUtc = completedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    /// <summary>The opaque id of the operation (the handle).</summary>
    public string OperationId { get; }

    /// <summary>The qualified, model-version-less id of the method, e.g. <c>System.Bot/TenantRepository.Dump-1</c>.</summary>
    public string MethodId { get; }

    /// <summary>The id of the invocation that started the operation (correlation with the handler log).</summary>
    public string? InvocationId { get; }

    /// <summary>The current state.</summary>
    public CkOperationState State { get; }

    /// <summary>The state as the service sent it (e.g. <c>RUNNING</c>); kept for states this client does not know.</summary>
    public string StateName { get; }

    /// <summary><c>true</c> for <see cref="CkOperationState.Succeeded" /> and <see cref="CkOperationState.Failed" />; a final operation never changes again.</summary>
    public bool IsFinal => State is CkOperationState.Succeeded or CkOperationState.Failed;

    /// <summary>The last progress report; <c>null</c> when the handler reported none.</summary>
    public CkOperationProgress? Progress { get; }

    /// <summary>
    ///     The result of a succeeded operation of a method with a result, in the JSON form of the method result; always
    ///     <c>null</c> for a result the CK model marks sensitive. Read it typed with
    ///     <see cref="CkOperationHandle{TResult}.ReadResult" />.
    /// </summary>
    public JsonElement? Result { get; }

    /// <summary>
    ///     The error of a failed operation: a platform code (<c>METHOD_*</c>, e.g. <c>METHOD_OUTCOME_UNKNOWN</c> with
    ///     <c>reason = handler-lost</c>) or a code the method declares.
    /// </summary>
    public CkMethodError? Error { get; }

    /// <summary>The time (UTC) the operation was accepted.</summary>
    public DateTime? AcceptedAtUtc { get; }

    /// <summary>The time (UTC) the handler started the work.</summary>
    public DateTime? StartedAtUtc { get; }

    /// <summary>The time (UTC) the operation became final.</summary>
    public DateTime? CompletedAtUtc { get; }

    /// <summary>The time (UTC) after which the operation is forgotten; <c>null</c> while it is open.</summary>
    public DateTime? ExpiresAtUtc { get; }
}

/// <summary>
///     One page of <c>runtime.operations</c> (AB#8446), newest accepted first.
/// </summary>
/// <param name="TotalCount">Number of operations that match the filter (all pages)</param>
/// <param name="Items">The operations of the page</param>
public sealed record CkOperationPage(long TotalCount, IReadOnlyList<CkOperation> Items);

/// <summary>
///     Filter of <see cref="CkOperationsClient.ListOperationsAsync" /> (AB#8446). All members are optional.
/// </summary>
public sealed class CkOperationListFilter
{
    /// <summary>Only operations of this method (qualified, version-less id, e.g. <c>System.Bot/TenantRepository.Dump-1</c>).</summary>
    public string? MethodId { get; init; }

    /// <summary>Only operations in this state; <see cref="CkOperationState.Unknown" /> is not a filter value.</summary>
    public CkOperationState? State { get; init; }

    /// <summary>Only operations accepted at or after this time (UTC).</summary>
    public DateTime? Since { get; init; }

    /// <summary>
    ///     <c>true</c> lists the operations of every caller of the tenant (tenant administrators only, otherwise
    ///     <c>OPERATION_QUERY_REFUSED</c>); default: the caller's own operations.
    /// </summary>
    public bool AllOwners { get; init; }

    /// <summary>Number of operations to skip; default 0.</summary>
    public int? Skip { get; init; }

    /// <summary>Page size (the service allows 1 to 200; default 50).</summary>
    public int? Take { get; init; }
}
