namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Report of <c>POST system/v1/tenants/reconcile-registry</c> (AB#6971, AB#6941): tenants that a parent lists in
///     its own registry but that are missing in the system registry.
/// </summary>
public class TenantRegistryReconcileReportDto
{
    /// <summary>
    ///     True when the missing rows were written; false for a dry run.
    /// </summary>
    public bool Applied { get; set; }

    /// <summary>
    ///     Tenants walked below the system tenant.
    /// </summary>
    public int Walked { get; set; }

    /// <summary>
    ///     Rows inserted (apply) or that would be inserted (dry run).
    /// </summary>
    public List<TenantRegistryReconcileInsertDto> Inserted { get; set; } = [];

    /// <summary>
    ///     Tenants whose system registry row already matches.
    /// </summary>
    public int AlreadyPresent { get; set; }

    /// <summary>
    ///     Rows left untouched, with the reason.
    /// </summary>
    public List<TenantRegistryReconcileConflictDto> Conflicts { get; set; } = [];

    /// <summary>
    ///     One-line summary.
    /// </summary>
    public string? Summary { get; set; }
}

/// <summary>
///     A system registry row inserted by the reconcile.
/// </summary>
public class TenantRegistryReconcileInsertDto
{
    /// <summary>
    ///     Tenant id.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    ///     Database name.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    ///     The tenant whose registry lists this tenant.
    /// </summary>
    public string? ParentTenantId { get; set; }
}

/// <summary>
///     A tenant the reconcile left untouched.
/// </summary>
public class TenantRegistryReconcileConflictDto
{
    /// <summary>
    ///     Tenant id.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    ///     The tenant whose registry lists this tenant.
    /// </summary>
    public string? ParentTenantId { get; set; }

    /// <summary>
    ///     Why the row was left alone.
    /// </summary>
    public string? Reason { get; set; }
}
