namespace Meshmakers.Octo.Communication.Contracts.MessageObjects;

/// <summary>
///     The invoker of a manual <see cref="ExecutePipelineRequest" />, carried from the communication
///     controller to the adapter so a <c>FromExecutePipelineCommand</c> pipeline runs as the user who
///     invoked it rather than anonymously (AB#5126, "carry the invoker through onto the execution
///     context"). A serialisable, transport-level mirror of the SDK's <c>VerifiedPrincipal</c>: this
///     contract assembly is lower than the pipeline SDK and cannot reference that type, so the node
///     maps this DTO onto a <c>VerifiedPrincipal</c> on arrival.
/// </summary>
/// <remarks>
///     <para>
///         🟢 <b>Additive &amp; back-compatible.</b> Every member is optional; a message published by
///         an older controller carries none of them and the pipeline runs anonymously exactly as
///         before.
///     </para>
///     <para>
///         🔴 <b>Token-free.</b> Like <c>VerifiedPrincipal</c>, this record carries no credential —
///         the invoker's raw token, when a node needs it for delegation, travels on the separate
///         <see cref="ExecutePipelineRequest.CallerAccessToken" /> field, never here, because the
///         principal is projected into the pipeline data root which is persisted and echoed.
///     </para>
/// </remarks>
public record ExecutePipelineCaller
{
    /// <summary>Subject id ("sub" claim) of the invoker, or null for a client-credentials token.</summary>
    public string? SubjectId { get; init; }

    /// <summary>Tenant id claim of the invoker, if present.</summary>
    public string? TenantId { get; init; }

    /// <summary>E-mail claim of the invoker, if present.</summary>
    public string? Email { get; init; }

    /// <summary>Display-name claim of the invoker, if present.</summary>
    public string? Name { get; init; }

    /// <summary>Role claims of the invoker.</summary>
    public IReadOnlyList<string> Roles { get; init; } = [];

    /// <summary>
    ///     Effective trust of the invoker as a <c>None=0 / Weak=1 / Strong=2</c> value (AB#5126). A
    ///     manually invoked pipeline is triggered with an authenticated bearer token, so a resolved
    ///     invoker is <c>Strong</c> (2).
    /// </summary>
    public int TrustLevel { get; init; }
}
