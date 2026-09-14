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
    public string ExecutionId { get; init; } = string.Empty;

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
               + $"'{TenantId}', adapter {AdapterRtId}, client '{ClientId}', expires {ExpiresAtUtc:O}";
    }
}
