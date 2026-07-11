namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Data transfer object describing a tenant's durable provisioning lifecycle state (AB#4348).
/// </summary>
public class TenantLifecycleDto
{
    /// <summary>Id of the tenant.</summary>
    public string? TenantId { get; set; }

    /// <summary>Database name of the tenant.</summary>
    public string? DatabaseName { get; set; }

    /// <summary>Lifecycle state: <c>Creating</c>, <c>Active</c>, <c>Deleting</c> or <c>Failed</c>.</summary>
    public string? State { get; set; }

    /// <summary>Sub-step reached within the <c>Creating</c> state.</summary>
    public string? Phase { get; set; }

    /// <summary>Number of setup / reconcile attempts so far.</summary>
    public int AttemptCount { get; set; }

    /// <summary>Last error observed while the tenant was not yet active.</summary>
    public string? LastError { get; set; }

    /// <summary>When the record was first created (UTC).</summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>When the state last changed (UTC).</summary>
    public DateTime LastTransitionUtc { get; set; }

    /// <summary>Owner of the current reconcile lease, if any.</summary>
    public string? LeaseOwner { get; set; }

    /// <summary>Expiry of the current reconcile lease, if any (UTC).</summary>
    public DateTime? LeaseUntil { get; set; }
}
