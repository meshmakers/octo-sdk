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
}
