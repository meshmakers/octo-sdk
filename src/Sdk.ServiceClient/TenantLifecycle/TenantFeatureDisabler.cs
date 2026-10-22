using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

namespace Meshmakers.Octo.Sdk.ServiceClient.TenantLifecycle;

/// <summary>
///     The four per-tenant capabilities a tenant delete is refused for while any of them is enabled (AB#4255).
/// </summary>
public enum TenantFeatureKind
{
    /// <summary>AI Services.</summary>
    AiServices,

    /// <summary>Communication (adapters, pools, pipelines).</summary>
    Communication,

    /// <summary>Reporting.</summary>
    Reporting,

    /// <summary>Stream Data (archives).</summary>
    StreamData
}

/// <summary>
///     The disable calls of the four capabilities, each issued with the token the caller holds <b>for the tenant that
///     is switched off</b> (AB#7192). The capability endpoints belong to the tenant's own administrator: a token of
///     the parent tenant is refused there.
/// </summary>
public interface ITenantFeatureSwitches
{
    /// <summary>Disables AI Services of the tenant.</summary>
    Task DisableAiAsync();

    /// <summary>Disables Communication of the tenant.</summary>
    Task DisableCommunicationAsync();

    /// <summary>Disables Reporting of the tenant.</summary>
    Task DisableReportingAsync();

    /// <summary>
    ///     Disables Stream Data of the tenant. The service refuses (409) while archives are still activated and names
    ///     them in the message.
    /// </summary>
    Task DisableStreamDataAsync();
}

/// <summary>
///     <see cref="ITenantFeatureSwitches" /> over four delegates, so a host composes the switches from whatever clients
///     it holds (octo-cli builds them from its service URLs, the MCP server from its client factory).
/// </summary>
public sealed class DelegateTenantFeatureSwitches(
    Func<Task> disableAi,
    Func<Task> disableCommunication,
    Func<Task> disableReporting,
    Func<Task> disableStreamData) : ITenantFeatureSwitches
{
    /// <inheritdoc />
    public Task DisableAiAsync() => disableAi();

    /// <inheritdoc />
    public Task DisableCommunicationAsync() => disableCommunication();

    /// <inheritdoc />
    public Task DisableReportingAsync() => disableReporting();

    /// <inheritdoc />
    public Task DisableStreamDataAsync() => disableStreamData();
}

/// <summary>Outcome of switching one capability off.</summary>
/// <param name="Feature">The capability.</param>
/// <param name="Disabled">True when the service accepted the disable.</param>
/// <param name="Error">The service's reason when it did not (for Stream Data this names the activated archives).</param>
public sealed record TenantFeatureDisableResult(TenantFeatureKind Feature, bool Disabled, string? Error);

/// <summary>What <see cref="TenantFeatureDisabler" /> did.</summary>
public sealed class TenantFeatureDisableReport
{
    /// <summary>Creates the report.</summary>
    public TenantFeatureDisableReport(IReadOnlyList<TenantFeatureDisableResult> results)
    {
        Results = results;
    }

    /// <summary>One entry per capability that was enabled, in the order they were switched off.</summary>
    public IReadOnlyList<TenantFeatureDisableResult> Results { get; }

    /// <summary>True when every enabled capability is now disabled (also when none was enabled).</summary>
    public bool AllDisabled => Results.All(r => r.Disabled);

    /// <summary>The capabilities that were enabled and are now disabled.</summary>
    public IEnumerable<TenantFeatureKind> Disabled => Results.Where(r => r.Disabled).Select(r => r.Feature);

    /// <summary>The capabilities that stay enabled, with the service's reason.</summary>
    public IEnumerable<TenantFeatureDisableResult> Failed => Results.Where(r => !r.Disabled);
}

/// <summary>
///     Switches off every capability a tenant has enabled, so the tenant can be deleted (AB#7192). Shared by octo-cli
///     <c>Delete --disable-features</c> and the MCP tool <c>delete_tenant</c> (<c>disableFeatures</c>).
/// </summary>
/// <remarks>
///     Reads nothing and deletes nothing itself: the caller reads the state (<see cref="TenantFeaturesStatusDto" />),
///     supplies the switches bound to the tenant's own token and deletes afterwards. Order: AI Services before
///     Communication (it runs on it), Stream Data last. A capability that refuses does not stop the others - the
///     report carries the reason, e.g. activated archives, which are reported and never removed here.
/// </remarks>
public static class TenantFeatureDisabler
{
    /// <summary>The display name used in messages.</summary>
    public static string DisplayName(TenantFeatureKind feature) => feature switch
    {
        TenantFeatureKind.AiServices => "AI Services",
        TenantFeatureKind.Communication => "Communication",
        TenantFeatureKind.Reporting => "Reporting",
        TenantFeatureKind.StreamData => "Stream Data",
        _ => throw new ArgumentOutOfRangeException(nameof(feature), feature, null)
    };

    /// <summary>The capabilities <paramref name="status" /> reports as enabled, in switch-off order.</summary>
    public static IReadOnlyList<TenantFeatureKind> GetEnabled(TenantFeaturesStatusDto status)
    {
        ArgumentNullException.ThrowIfNull(status);

        var enabled = new List<TenantFeatureKind>();
        if (status.AiServices?.TenantEnabled == true)
        {
            enabled.Add(TenantFeatureKind.AiServices);
        }

        if (status.Communication?.TenantEnabled == true)
        {
            enabled.Add(TenantFeatureKind.Communication);
        }

        if (status.Reporting?.TenantEnabled == true)
        {
            enabled.Add(TenantFeatureKind.Reporting);
        }

        if (status.StreamData?.TenantEnabled == true)
        {
            enabled.Add(TenantFeatureKind.StreamData);
        }

        return enabled;
    }

    /// <summary>Switches off every capability <paramref name="status" /> reports as enabled.</summary>
    public static async Task<TenantFeatureDisableReport> DisableEnabledAsync(TenantFeaturesStatusDto status,
        ITenantFeatureSwitches switches)
    {
        ArgumentNullException.ThrowIfNull(switches);

        var results = new List<TenantFeatureDisableResult>();
        foreach (var feature in GetEnabled(status))
        {
            try
            {
                await (feature switch
                {
                    TenantFeatureKind.AiServices => switches.DisableAiAsync(),
                    TenantFeatureKind.Communication => switches.DisableCommunicationAsync(),
                    TenantFeatureKind.Reporting => switches.DisableReportingAsync(),
                    TenantFeatureKind.StreamData => switches.DisableStreamDataAsync(),
                    _ => throw new ArgumentOutOfRangeException(nameof(feature), feature, null)
                });
                results.Add(new TenantFeatureDisableResult(feature, true, null));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                results.Add(new TenantFeatureDisableResult(feature, false, ex.Message));
            }
        }

        return new TenantFeatureDisableReport(results);
    }
}
