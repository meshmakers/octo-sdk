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
///         🔴 <b>Since AB#4924 a SECOND secret travels here: <see cref="DatabasePassword" />.</b> The
///         lease is what gives a pool member access to the borrower's <i>data</i>, not only to its
///         identity. The operator deliberately withholds the cluster's shared data-store credentials
///         from an adapter pool (<c>WorkloadReconciler.AppendClusterSecrets</c>), on the stated ground
///         that "tenant-scoped data access arrives with the lease and leaves with it" — a sentence
///         that only became true when <see cref="DatabaseName" />, <see cref="DatabaseUser" /> and
///         <see cref="DatabasePassword" /> existed. Every rule written above for
///         <see cref="ClientSecret" /> applies to <see cref="DatabasePassword" /> unchanged, and the
///         assertions on both sides name it <b>explicitly</b> rather than trusting the older one to
///         cover it: a test that would still pass if only <see cref="ClientSecret" /> were redacted
///         proves nothing about the second secret.
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
    ///     Name of the borrowing tenant's <b>database</b> — the scope the credential below is valid
    ///     for, and nothing else.
    /// </summary>
    /// <remarks>
    ///     🔴 <b>Not a credential, and not decoration.</b> A pool member has to know which database
    ///     <see cref="DatabaseUser" /> may be presented to, because it also opens databases that are
    ///     <i>not</i> the borrower's — the installation's tenant registry above all. Presenting the
    ///     borrower's datasource user there would fail, and presenting a shared one to the borrower's
    ///     database is exactly what this whole mechanism removes. So the credential is installed for
    ///     one named database and is invisible everywhere else.
    ///     <para>
    ///         It comes from the controller rather than from the member for one reason: resolving
    ///         tenant → database means reading the tenant registry, and on a pool member that read is
    ///         itself gated by a credential the member does not have until the lease installs one. The
    ///         controller is the only party that can break that circle.
    ///     </para>
    /// </remarks>
    public string DatabaseName { get; init; } = string.Empty;

    /// <summary>
    ///     The MongoDB user of the borrowing tenant's database, resolved by the controller.
    /// </summary>
    /// <remarks>
    ///     🔴 <b>Resolved, never derived here.</b> The installation names its per-tenant datasource
    ///     users by a format over the database name (<c>octo-system-ds-user-{0}</c>), and the
    ///     temptation is to send only the password and let the member format the name. That would put
    ///     a second copy of the naming rule in a process that must not be able to name any database
    ///     but the one it was lent — one edit away from a member that constructs a user for a
    ///     neighbour tenant and finds the shared password still fits. The user and the password are
    ///     therefore two independent fields, both filled by the controller.
    /// </remarks>
    public string DatabaseUser { get; init; } = string.Empty;

    /// <summary>
    ///     🔴 Password of <see cref="DatabaseUser" />, in plaintext. Same rules as
    ///     <see cref="ClientSecret" />: lease-scoped, never persisted, never logged, and deliberately
    ///     not rendered by <see cref="ToString" />.
    /// </summary>
    /// <remarks>
    ///     <b>Today this value is installation-wide</b> — one password behind every per-database user
    ///     — and the controller's resolver says so at the line that reads it. That is a property of
    ///     where the password comes from, not of this contract: <b>AB#5255</b> gives each database its
    ///     own password and changes only that resolver. Nothing on this wire and nothing on the member
    ///     may assume the two are the same value, or the follow-up becomes a second migration instead
    ///     of a one-line change.
    /// </remarks>
    public string DatabasePassword { get; init; } = string.Empty;

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
    ///     🔴 Deliberately prints neither <see cref="ClientSecret" /> nor
    ///     <see cref="DatabasePassword" />. See the remarks on the type.
    /// </summary>
    /// <remarks>
    ///     The two <i>identities</i> are printed — <see cref="ClientId" /> and the database this lease
    ///     opens, as which user — because that is what makes a lease log line diagnosable at all.
    ///     Their secrets are not.
    /// </remarks>
    public override string ToString()
    {
        return $"Lease '{LeaseId}' of pool {PoolRtId} (tenant '{PoolTenantId}') to tenant "
               + $"'{TenantId}', adapter {AdapterRtId}, client '{ClientId}', database "
               + $"'{(string.IsNullOrEmpty(DatabaseName) ? "<none>" : DatabaseName)}' as "
               + $"'{(string.IsNullOrEmpty(DatabaseUser) ? "<none>" : DatabaseUser)}', pipeline "
               + $"{(string.IsNullOrEmpty(PipelineRtId) ? "<none>" : PipelineRtId)}, execution "
               + $"'{ExecutionId}', expires {ExpiresAtUtc:O}";
    }
}
