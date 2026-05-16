namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Payload for the operator's <c>WorkloadDeployedAsync</c> callback. Carries
/// everything the operator needs to <c>helm upgrade --install</c> a workload:
/// chart reference (resolved from the workload's <c>HelmRepositoryConfiguration</c>),
/// base values, and structured overrides. Secret-flagged overrides are
/// decrypted by the controller before being placed on the wire — the wire
/// itself is SignalR-over-TLS.
/// </summary>
public record WorkloadDeployedDto
{
    /// <summary>
    /// Tenant the workload belongs to.
    /// </summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>
    /// Pool that manages this workload. The operator deploys into the pool's
    /// Kubernetes namespace.
    /// </summary>
    public string PoolName { get; init; } = string.Empty;

    /// <summary>
    /// Workload name (used as the basis for the Helm release name).
    /// </summary>
    public string WorkloadName { get; init; } = string.Empty;

    /// <summary>
    /// Runtime entity id of the workload. The operator projects this into
    /// the rendered Helm values as <c>adapterRtId</c> so the adapter pod
    /// can identify itself to the controller without the operator (or the
    /// CK author) having to maintain the value by hand in
    /// <see cref="ValuesYaml"/>.
    /// </summary>
    public string WorkloadRtId { get; init; } = string.Empty;

    /// <summary>
    /// Discriminator between <c>Adapter</c> and <c>Application</c>. The
    /// operator uses this to apply type-specific defaults if needed.
    /// </summary>
    public WorkloadTypeDto WorkloadType { get; init; }

    /// <summary>
    /// Helm repository URL the chart is pulled from. Typically a public or
    /// private GitHub Pages site. Resolved from the workload's
    /// <c>HelmRepositoryConfiguration</c> by the controller.
    /// </summary>
    public string RepositoryUrl { get; init; } = string.Empty;

    /// <summary>
    /// Optional basic-auth username for private repositories.
    /// </summary>
    public string? RepositoryUsername { get; init; }

    /// <summary>
    /// Optional basic-auth password / PAT for private repositories. Decrypted
    /// server-side before being placed on the wire.
    /// </summary>
    public string? RepositoryPassword { get; init; }

    /// <summary>
    /// Chart name within the repository (e.g. <c>voest-app</c>).
    /// </summary>
    public string ChartName { get; init; } = string.Empty;

    /// <summary>
    /// Chart version (e.g. <c>1.2.3</c>).
    /// </summary>
    public string ChartVersion { get; init; } = string.Empty;

    /// <summary>
    /// Base <c>values.yaml</c> content as a YAML string. May be empty when
    /// only structured overrides are used. Acts as the base layer of Helm
    /// values; <see cref="Values"/> is deep-merged on top.
    /// </summary>
    public string ValuesYaml { get; init; } = string.Empty;

    /// <summary>
    /// Structured value overrides. Secret-flagged entries have their
    /// <see cref="ValueOverrideDto.Value"/> already decrypted.
    /// </summary>
    public IReadOnlyList<ValueOverrideDto> Values { get; init; } = Array.Empty<ValueOverrideDto>();

    /// <summary>
    /// When true, the operator may inject cluster-internal credentials
    /// (MongoDB user/admin passwords, CrateDB password, RabbitMQ password)
    /// from its own configuration as secret-flagged value overrides at
    /// deploy time. Only relevant for adapters running in the same cluster
    /// as the OctoMesh core services. Defaults to false so untrusted /
    /// edge workloads do not receive cluster credentials by accident.
    /// </summary>
    public bool ReceivesClusterSecrets { get; init; }
}
