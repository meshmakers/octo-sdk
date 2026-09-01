namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Answer of <c>POST {tenantId}/v1/adapter/{adapterRtId}/serviceAccount/rotateSecret</c>
///     (AB#5032, client surface AB#5048) — the mirror of the communication controller's
///     <c>RotateServiceAccountSecretResultDto</c>.
/// </summary>
/// <remarks>
///     🔴 It deliberately carries <b>no secret</b>, and adding one here would defeat the decision
///     taken server-side: the plaintext lives in exactly two places — the tenant's
///     <c>ServiceAccountConfiguration</c> entity and the identity client's hash — and a third copy
///     travelling back through the SDK would end up in proxy logs, shell history and CI output.
///     Everything a caller needs in order to act is in <see cref="RequiresPipelineRedeploy" /> and
///     <see cref="Message" />.
/// </remarks>
/// <param name="ClientId">The identity client whose secret was replaced.</param>
/// <param name="ConfigurationWellKnownName">
///     <c>RtWellKnownName</c> of the configuration entity holding the new secret — the key the mesh
///     adapter resolves its execution identity by.
/// </param>
/// <param name="WasCreated">
///     <c>true</c> when the adapter had no service account yet and the call provisioned one instead
///     of rotating. Nothing was invalidated in that case.
/// </param>
/// <param name="RequiresPipelineRedeploy">
///     <c>true</c> when the adapter's pipelines / data flows must be redeployed before the new
///     secret takes effect — the adapter caches the credentials in the pipeline's
///     <c>GlobalConfiguration</c> at registration time and never refreshes them. A caller that
///     drops this flag produces the "rotation done, still broken" situation.
/// </param>
/// <param name="Message">Operator-facing summary, including the redeploy instruction when one is needed.</param>
public sealed record RotateServiceAccountSecretResultDto(
    string ClientId,
    string ConfigurationWellKnownName,
    bool WasCreated,
    bool RequiresPipelineRedeploy,
    string Message);
