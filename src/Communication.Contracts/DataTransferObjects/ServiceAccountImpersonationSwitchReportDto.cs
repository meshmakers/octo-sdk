namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Answer of <c>POST {tenantId}/v1/serviceAccount/switchToImpersonation</c> (AB#5537, client
///     surface AB#6642) — the mirror of the communication controller's
///     <c>ServiceAccountImpersonationSwitchReportDto</c>.
/// </summary>
/// <remarks>
///     🔴 It deliberately carries <b>no secret</b> and no token: the report names accounts, client
///     ids and outcomes only. Adding a secret-bearing member here would put a credential into
///     proxy logs, shell history and CI output.
/// </remarks>
/// <param name="TenantId">The tenant the switch ran on.</param>
/// <param name="Applied"><c>false</c> = dry run, nothing was written.</param>
/// <param name="Accounts">One entry per <c>ServiceAccountConfiguration</c> of the tenant.</param>
/// <param name="RedeployedDataFlowRtIds">Data flows redeployed after the switch.</param>
/// <param name="FailedDataFlowRedeploys">Data flows whose redeploy failed — to be redeployed by hand.</param>
public sealed record ServiceAccountImpersonationSwitchReportDto(
    string TenantId,
    bool Applied,
    IReadOnlyList<ServiceAccountImpersonationSwitchEntryDto> Accounts,
    IReadOnlyList<string> RedeployedDataFlowRtIds,
    IReadOnlyList<DataFlowRedeployFailureDto> FailedDataFlowRedeploys);

/// <summary>
///     One account of the impersonation switch report (AB#5537).
/// </summary>
/// <param name="ConfigurationRtId">The account's runtime id.</param>
/// <param name="WellKnownName">Its well-known name.</param>
/// <param name="ClientId">Its identity client id, when known.</param>
/// <param name="Outcome">
///     One of <see cref="ServiceAccountImpersonationSwitchOutcomes" />: <c>WouldSwitch</c>,
///     <c>Switched</c>, <c>SwitchedPendingInvalidation</c>, <c>AlreadyImpersonating</c>,
///     <c>SkippedAdapterOwnIdentity</c>, <c>SkippedNoCapableActor</c>, <c>SkippedIncomplete</c> or
///     <c>Failed</c>. Kept as a string so a newer server outcome does not break an older client.
/// </param>
/// <param name="Message">Reason in plain words.</param>
/// <param name="PipelineRtIds">Pipelines using the account.</param>
public sealed record ServiceAccountImpersonationSwitchEntryDto(
    string ConfigurationRtId,
    string WellKnownName,
    string? ClientId,
    string Outcome,
    string Message,
    IReadOnlyList<string> PipelineRtIds);

/// <summary>
///     A data flow whose redeploy after the impersonation switch failed (AB#5537).
/// </summary>
/// <param name="DataFlowRtId">The data flow's runtime id.</param>
/// <param name="Reason">Exception type and message.</param>
public sealed record DataFlowRedeployFailureDto(string DataFlowRtId, string Reason);

/// <summary>
///     The outcome values of <see cref="ServiceAccountImpersonationSwitchEntryDto.Outcome" /> the
///     controller currently reports.
/// </summary>
public static class ServiceAccountImpersonationSwitchOutcomes
{
    /// <summary>Dry run: the account would be switched.</summary>
    public const string WouldSwitch = "WouldSwitch";

    /// <summary>The account was switched.</summary>
    public const string Switched = "Switched";

    /// <summary>Switched; the old secret is invalidated once the data flows are redeployed.</summary>
    public const string SwitchedPendingInvalidation = "SwitchedPendingInvalidation";

    /// <summary>The account already impersonates.</summary>
    public const string AlreadyImpersonating = "AlreadyImpersonating";

    /// <summary>The account is the adapter's own identity and is left alone.</summary>
    public const string SkippedAdapterOwnIdentity = "SkippedAdapterOwnIdentity";

    /// <summary>No actor is capable of impersonating for this account.</summary>
    public const string SkippedNoCapableActor = "SkippedNoCapableActor";

    /// <summary>The account configuration is incomplete.</summary>
    public const string SkippedIncomplete = "SkippedIncomplete";

    /// <summary>The switch of this account failed.</summary>
    public const string Failed = "Failed";

    /// <summary>All outcomes, in report order.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        WouldSwitch, Switched, SwitchedPendingInvalidation, AlreadyImpersonating,
        SkippedAdapterOwnIdentity, SkippedNoCapableActor, SkippedIncomplete, Failed
    ];
}
