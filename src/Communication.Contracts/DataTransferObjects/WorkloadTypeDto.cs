namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Discriminator for the kind of workload being deployed. Mirrors the CK type
/// hierarchy: every subtype of <c>System.Communication/DeployableWorkload</c>
/// maps to one of these values.
/// </summary>
public enum WorkloadTypeDto
{
    /// <summary>
    /// An <c>Adapter</c> — ETL pipeline executor that connects back to the
    /// controller via SignalR.
    /// </summary>
    Adapter = 0,

    /// <summary>
    /// An <c>Application</c> — tenant-specific web app deployed by the
    /// Communication Operator into the pool's Kubernetes namespace.
    /// </summary>
    Application = 1,

    /// <summary>
    /// An <c>AdapterPool</c> (AB#4924) — one workload with a replica range whose members are
    /// leased to tenants in the owning tenant's subtree, one work item per lease. It is a
    /// <c>DeployableWorkload</c> like the other two and goes through the same
    /// deploy / undeploy / scale path, but the operator treats it differently in three
    /// respects, which is the only reason this discriminator exists:
    /// it is deployed into the <b>platform namespace</b> rather than the namespace tenant
    /// workloads go to, it carries an owner reference to the lending tenant so a deleted
    /// tenant garbage-collects its pool, and it never receives the cluster's shared data-store
    /// credentials — a process that executes other tenants' work must not hold a standing
    /// credential to every tenant's data.
    ///
    /// Appended, never inserted: the value travels on the wire as an integer and an operator
    /// that pre-dates it only uses <c>WorkloadType</c> for log output.
    /// </summary>
    AdapterPool = 2,
}
