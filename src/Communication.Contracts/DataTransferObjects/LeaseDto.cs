namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     One lease of an adapter pool member to a borrowing tenant (AB#4924, concept §4).
/// </summary>
/// <remarks>
///     <para>
///         A lease binds a pool member — a process that belongs to no tenant — to exactly one
///         borrowing tenant for the duration of one work item. The member loads that tenant, executes,
///         reports the outcome through <c>IAdapterPoolHub.ReleaseLeaseAsync</c> and drops every
///         tenant-scoped thing it built. The isolation invariant is a property of <b>time</b>: the
///         process serves exactly one tenant at any instant.
///     </para>
///     <para>
///         🔴 <b>This object carries a client secret.</b> Concept §8 Q6 decided the borrower identity:
///         the lease carries the <b>borrower's own</b> <c>PipelineServiceAccount</c> credential
///         (AB#5027), so the member logs in and acts exactly as the borrower's own adapter would. No
///         standing grant is created in any borrower tenant and the whole AB#5027 identity chain is
///         reused unchanged. RFC 8693 token exchange does <b>not</b> cover this case — it rejects a
///         subject token without <c>sub</c>/<c>tenant_id</c>, which is precisely the shape of a
///         client-credentials token, and it mints an <c>xt_</c> shadow user, which is not what a
///         service identity wants.
///     </para>
///     <para>
///         Two consequences the implementation must honour and which are pinned by tests on both
///         sides: <see cref="ClientSecret" /> is <b>TTL-scoped and never persisted</b> by the member,
///         and it must never reach a log target. <see cref="ToString" /> is overridden here for that
///         reason — a record's generated <c>ToString</c> prints every property, and structured logging
///         a <c>LeaseDto</c> would otherwise put the secret in the log the first time somebody adds a
///         diagnostic line.
///     </para>
///     <para>
///         🔴 <b>Since AB#4924 §9.9 / D4 the same rule covers the work the lease carries.</b>
///         <see cref="PipelineInput" /> is the borrowing tenant's payload, and <see cref="Pipeline" />
///         carries that pipeline's resolved configuration entries, which since AB#5027 include a
///         service-account credential of their own. Neither is rendered: <see cref="ToString" /> names
///         identifiers only, and the assertions on both sides now cover the enlarged object rather
///         than only <see cref="ClientSecret" />.
///     </para>
/// </remarks>
public record LeaseDto
{
    /// <summary>
    ///     Identifier of this lease. Echoed back on <c>ReleaseLeaseAsync</c> so a release that arrives
    ///     after the controller already expired the lease can be recognised as stale rather than
    ///     released against whatever lease the member holds now.
    /// </summary>
    public string LeaseId { get; init; } = string.Empty;

    /// <summary>
    ///     The <b>borrowing</b> tenant. This is the tenant the member acts for while the lease is
    ///     held: the tenant repository it loads, the CK model it caches, and the <c>tenant_id</c> of
    ///     the token it presents.
    /// </summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>
    ///     The <b>lending</b> tenant — the owner of the <c>AdapterPool</c> the member belongs to.
    ///     Recorded on the borrower's execution as <c>LeasedFromTenantId</c>.
    /// </summary>
    public string PoolTenantId { get; init; } = string.Empty;

    /// <summary>
    ///     RtId of the <c>AdapterPool</c> in <see cref="PoolTenantId" />. Bare 24-character hex.
    /// </summary>
    public string PoolRtId { get; init; } = string.Empty;

    /// <summary>
    ///     RtId of the borrowing tenant's <c>Adapter</c> entity (the one whose <c>LifecycleMode</c> is
    ///     <c>Leased</c>). The member registers under this identity for the duration of the lease, so
    ///     the pipelines it loads and the executions it reports are the ones that adapter owns.
    /// </summary>
    public string AdapterRtId { get; init; } = string.Empty;

    /// <summary>
    ///     CkTypeId of the borrowing tenant's <c>Adapter</c> entity. Adapter is polymorphic, so the
    ///     bare RtId is not enough to build an <c>RtEntityId</c>.
    /// </summary>
    public string AdapterCkTypeId { get; init; } = string.Empty;

    /// <summary>
    ///     The pipeline execution this lease was granted for, when the lease is tied to one. Empty
    ///     while leasing is driven by hand (increment 6); the scheduler (increment 7) always sets it.
    /// </summary>
    /// <remarks>
    ///     🔴 <b>This id names an execution entity that already exists.</b> The controller created it
    ///     <c>Queued</c> at enqueue and moved it to <c>Running</c> when it claimed it for this lease.
    ///     The member executes <b>against</b> it and must never report an execution start for it: the
    ///     start-report path (<c>PipelineExecutionService.StartExecutionAsync</c>) inserts a new entity
    ///     with a new RtId and never looks an existing one up by <c>ExecutionId</c>, so a member that
    ///     reported a start would produce a second entity for one piece of work — two billing spans
    ///     (concept §4b) and a queue history that no longer joins up (AB#4924 §9.9 / D4).
    /// </remarks>
    public string ExecutionId { get; init; } = string.Empty;

    /// <summary>
    ///     RtId of the pipeline the member is to run, bare 24-character hex. Empty when the lease
    ///     carries no work — a hand-driven lease (increment 6) still exists and is still valid.
    /// </summary>
    /// <remarks>
    ///     AB#4924 §9.9 / D4: increment 7 queued, rotated, granted and reaped correctly, and a member
    ///     that received a lease still had nothing to run. The lease names the work explicitly rather
    ///     than the member inferring it — the scheduler decided <i>which</i> queued item this lease
    ///     serves, and a member re-deriving that from the tenant's queue could pick a different one.
    /// </remarks>
    public string PipelineRtId { get; init; } = string.Empty;

    /// <summary>
    ///     The pipeline input of the queued work item — <c>RtPipelineExecution.InputData</c>, verbatim.
    ///     Null when the trigger supplied none.
    /// </summary>
    /// <remarks>
    ///     🔴 Never rendered by <see cref="ToString" />. It is caller-supplied payload of the borrowing
    ///     tenant and has no business in a log line of a process that serves another tenant a second
    ///     later.
    /// </remarks>
    public string? PipelineInput { get; init; }

    /// <summary>
    ///     What the member needs in order to run that pipeline: the same
    ///     <see cref="PipelineConfigurationDto" /> element a dedicated adapter receives in its
    ///     <c>AdapterConfigurationDto.Pipelines</c> — the node definition plus the pipeline's resolved
    ///     configuration entries. Null when the lease carries no work.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         🔴 <b>The lease carries it because only the controller can produce it.</b> The
    ///         projection (<c>AdapterService.CreatePipelineConfigurationAsync</c>) injects the
    ///         adapter's default pipeline service account, projects the tenant's Signal channel and
    ///         resolves the deploy-time <c>{{service.authority}}</c> token. A member rebuilding that
    ///         from the borrower's entities would be a second implementation of a projection that
    ///         already exists, free to drift from it silently.
    ///     </para>
    ///     <para>
    ///         <b>Size.</b> This travels controller → member, which is a SignalR
    ///         <i>server-to-client</i> send; <c>HubOptions.MaximumReceiveMessageSize</c> governs what
    ///         the <i>server receives</i>, and the controller raises it to 100 MiB anyway
    ///         (<c>Program.cs</c>). The identical shape already ships in production:
    ///         <c>IAdapterHubCallbacks.AdapterConfigurationUpdatedAsync</c> pushes a whole
    ///         <c>AdapterConfigurationDto</c> — <i>every</i> deployed pipeline of an adapter — down the
    ///         same kind of channel. One pipeline is strictly less than that.
    ///     </para>
    ///     <para>
    ///         🔴 Like <see cref="ClientSecret" />, the configuration entries can carry credential
    ///         material — AB#5027 puts the pipeline service account in exactly here — so this property
    ///         is not rendered by <see cref="ToString" /> either.
    ///     </para>
    /// </remarks>
    public PipelineConfigurationDto? Pipeline { get; init; }

    /// <summary>
    ///     Client id of the borrower's <c>PipelineServiceAccount</c> (AB#5027).
    /// </summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    ///     🔴 Client secret of the borrower's <c>PipelineServiceAccount</c>, in plaintext.
    ///     Lease-scoped: the member uses it to obtain a token with
    ///     <c>acr_values=tenant:{TenantId}</c> and must drop it — and the token — on release. It must
    ///     never be written to disk, to configuration, or to a log.
    /// </summary>
    public string ClientSecret { get; init; } = string.Empty;

    /// <summary>
    ///     When the controller granted the lease (UTC). Together with the release it is the span the
    ///     member was held for this borrower — the billing input of concept §4b.
    /// </summary>
    public DateTime GrantedAtUtc { get; init; }

    /// <summary>
    ///     When the lease expires (UTC). After this instant the controller may re-queue the work and
    ///     drain the member, because its post-lease cleanliness is unproven (concept §6).
    /// </summary>
    public DateTime ExpiresAtUtc { get; init; }

    /// <summary>
    ///     🔴 Deliberately does not print <see cref="ClientSecret" />. See the remarks on the type.
    /// </summary>
    public override string ToString()
    {
        return $"Lease '{LeaseId}' of pool {PoolRtId} (tenant '{PoolTenantId}') to tenant "
               + $"'{TenantId}', adapter {AdapterRtId}, client '{ClientId}', pipeline "
               + $"{(string.IsNullOrEmpty(PipelineRtId) ? "<none>" : PipelineRtId)}, execution "
               + $"'{ExecutionId}', expires {ExpiresAtUtc:O}";
    }
}
