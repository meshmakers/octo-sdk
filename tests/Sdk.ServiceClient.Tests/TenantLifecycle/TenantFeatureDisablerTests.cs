using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.ServiceClient.TenantLifecycle;

namespace Sdk.ServiceClient.Tests.TenantLifecycle;

/// <summary>
///     AB#7192: <c>Delete --disable-features</c> / <c>delete_tenant disableFeatures</c> switch off exactly the
///     capabilities the tenant has enabled, in a fixed order, and report what stayed enabled and why.
/// </summary>
public class TenantFeatureDisablerTests
{
    private static TenantFeaturesStatusDto Status(bool ai, bool communication, bool reporting, bool streamData) =>
        new()
        {
            AiServices = new TenantFeatureStatusDto { TenantEnabled = ai },
            Communication = new TenantFeatureStatusDto { TenantEnabled = communication },
            Reporting = new TenantFeatureStatusDto { TenantEnabled = reporting },
            StreamData = new StreamDataFeatureStatusDto { InstanceEnabled = true, TenantEnabled = streamData }
        };

    private sealed class RecordingSwitches : ITenantFeatureSwitches
    {
        public List<TenantFeatureKind> Calls { get; } = [];
        public Func<TenantFeatureKind, Exception?> Failure { get; init; } = _ => null;

        private Task Record(TenantFeatureKind kind)
        {
            Calls.Add(kind);
            var failure = Failure(kind);
            return failure == null ? Task.CompletedTask : Task.FromException(failure);
        }

        public Task DisableAiAsync() => Record(TenantFeatureKind.AiServices);
        public Task DisableCommunicationAsync() => Record(TenantFeatureKind.Communication);
        public Task DisableReportingAsync() => Record(TenantFeatureKind.Reporting);
        public Task DisableStreamDataAsync() => Record(TenantFeatureKind.StreamData);
    }

    [Fact]
    public async Task DisableEnabledAsync_AllEnabled_SwitchesOffInOrder_AiFirstStreamDataLast()
    {
        var switches = new RecordingSwitches();

        var report = await TenantFeatureDisabler.DisableEnabledAsync(Status(true, true, true, true), switches);

        Assert.Equal(
            [TenantFeatureKind.AiServices, TenantFeatureKind.Communication, TenantFeatureKind.Reporting,
                TenantFeatureKind.StreamData], switches.Calls);
        Assert.True(report.AllDisabled);
        Assert.Equal(4, report.Disabled.Count());
        Assert.Empty(report.Failed);
    }

    [Fact]
    public async Task DisableEnabledAsync_OnlyTouchesWhatIsEnabled()
    {
        var switches = new RecordingSwitches();

        var report = await TenantFeatureDisabler.DisableEnabledAsync(Status(false, true, false, false), switches);

        Assert.Equal([TenantFeatureKind.Communication], switches.Calls);
        Assert.Equal([TenantFeatureKind.Communication], report.Disabled);
    }

    [Fact]
    public async Task DisableEnabledAsync_NothingEnabled_CallsNothingAndIsAllDisabled()
    {
        var switches = new RecordingSwitches();

        var report = await TenantFeatureDisabler.DisableEnabledAsync(Status(false, false, false, false), switches);

        Assert.Empty(switches.Calls);
        Assert.True(report.AllDisabled);
        Assert.Empty(report.Results);
    }

    [Fact]
    public async Task DisableEnabledAsync_UnreportedFeatures_AreNotTreatedAsEnabled()
    {
        var switches = new RecordingSwitches();

        var report = await TenantFeatureDisabler.DisableEnabledAsync(new TenantFeaturesStatusDto(), switches);

        Assert.Empty(switches.Calls);
        Assert.True(report.AllDisabled);
    }

    [Fact]
    public async Task DisableEnabledAsync_StreamDataRefusedForActivatedArchives_IsReported_AndDoesNotStopTheOthers()
    {
        var switches = new RecordingSwitches
        {
            Failure = kind => kind == TenantFeatureKind.Communication
                ? new InvalidOperationException("Conflict: activated archives: rawdata")
                : null
        };

        var report = await TenantFeatureDisabler.DisableEnabledAsync(Status(false, true, true, true), switches);

        Assert.Equal(
            [TenantFeatureKind.Communication, TenantFeatureKind.Reporting, TenantFeatureKind.StreamData],
            switches.Calls);
        Assert.False(report.AllDisabled);
        var failed = Assert.Single(report.Failed);
        Assert.Equal(TenantFeatureKind.Communication, failed.Feature);
        Assert.Contains("activated archives: rawdata", failed.Error);
        Assert.Equal([TenantFeatureKind.Reporting, TenantFeatureKind.StreamData], report.Disabled);
    }

    [Fact]
    public async Task DisableEnabledAsync_Cancellation_IsNotSwallowed()
    {
        var switches = new RecordingSwitches { Failure = _ => new OperationCanceledException() };

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            TenantFeatureDisabler.DisableEnabledAsync(Status(true, false, false, false), switches));
    }

    [Theory]
    [InlineData(TenantFeatureKind.AiServices, "AI Services")]
    [InlineData(TenantFeatureKind.Communication, "Communication")]
    [InlineData(TenantFeatureKind.Reporting, "Reporting")]
    [InlineData(TenantFeatureKind.StreamData, "Stream Data")]
    public void DisplayName_NamesEveryCapability(TenantFeatureKind kind, string expected)
    {
        Assert.Equal(expected, TenantFeatureDisabler.DisplayName(kind));
    }

    [Fact]
    public async Task DelegateSwitches_CallTheDelegateOfTheCapability()
    {
        var calls = new List<string>();
        var switches = new DelegateTenantFeatureSwitches(
            () => { calls.Add("ai"); return Task.CompletedTask; },
            () => { calls.Add("communication"); return Task.CompletedTask; },
            () => { calls.Add("reporting"); return Task.CompletedTask; },
            () => { calls.Add("streamdata"); return Task.CompletedTask; });

        var report = await TenantFeatureDisabler.DisableEnabledAsync(Status(true, true, false, true), switches);

        Assert.Equal(["ai", "communication", "streamdata"], calls);
        Assert.True(report.AllDisabled);
    }
}
