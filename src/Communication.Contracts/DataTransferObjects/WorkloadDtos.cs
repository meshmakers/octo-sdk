namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Subset of an <c>RtDeployableWorkload</c> (Adapter or Application) returned
/// by <c>GET {tenantId}/v1/workload?chartName=…</c>. Carries the fields a
/// CI rollout script needs to drive a per-tenant update — enough to skip the
/// tenant when empty, or to PATCH the chart version and POST the deploy when
/// present. Backend serializes this from a controller-local record with the
/// same JSON shape; mirror it here in PascalCase so SDK callers get the
/// idiomatic .NET property names.
/// </summary>
public sealed record WorkloadSummaryDto(
    string RtId,
    string Name,
    string CkTypeId,
    string ChartName,
    string CurrentChartVersion,
    string DeploymentState);

/// <summary>
/// Body for <c>PATCH {tenantId}/v1/workload/{workloadRtId}/chart-version</c>.
/// The server validates the value matches a SemVer regex before persisting.
/// </summary>
public sealed record UpdateChartVersionDto(string ChartVersion);
