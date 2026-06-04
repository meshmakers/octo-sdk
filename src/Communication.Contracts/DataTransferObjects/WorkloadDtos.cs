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

/// <summary>
/// One named public base domain available to workloads via the
/// <c>{{domain.NAME}}</c> placeholder syntax in their <c>Hostname</c>
/// attribute. Returned by <c>GET {tenantId}/v1/communication/domains</c>
/// so the Refinery Studio can offer the configured choices in workload
/// edit forms instead of forcing free-text entry.
/// </summary>
/// <param name="Name">Lookup key referenced by templates, e.g. <c>default</c> in <c>adapter.{{domain.default}}</c>.</param>
/// <param name="BaseDomain">Resolved base domain (no scheme, no leading dot), e.g. <c>staging.octo-mesh.com</c>.</param>
public sealed record DomainConfigurationDto(string Name, string BaseDomain);

/// <summary>
/// One template placeholder available to workloads' <c>Hostname</c>,
/// non-secret <c>ValueOverride.Value</c> and <c>ValuesYaml</c>. Returned by
/// <c>GET {tenantId}/v1/communication/workload-variables</c> so the Refinery
/// Studio can offer a suggestion list across all three families
/// (<c>{{domain.NAME}}</c>, <c>{{service.NAME}}</c>, <c>{{context.tenantId}}</c>)
/// instead of forcing free-text entry.
/// </summary>
/// <param name="Placeholder">Full template token including braces, e.g. <c>{{service.authority}}</c>.</param>
/// <param name="Description">Human-readable description of what the placeholder resolves to.</param>
/// <param name="SampleValue">Configured value snapshot for cluster-config-driven placeholders. <c>null</c> for per-deploy values like <c>{{context.tenantId}}</c>.</param>
public sealed record WorkloadVariableDto(string Placeholder, string Description, string? SampleValue);
