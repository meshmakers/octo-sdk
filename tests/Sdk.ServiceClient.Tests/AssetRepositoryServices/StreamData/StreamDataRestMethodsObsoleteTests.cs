using System.Reflection;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.StreamData;

namespace Sdk.ServiceClient.Tests.AssetRepositoryServices.StreamData;

/// <summary>
///     AB#9059 (part 2, after the CLI/MCP switch of AB#9060): the archive operations of
///     <see cref="IStreamDataServicesClient" /> that the CK methods of <c>System.StreamData</c> replace are obsolete on the
///     interface and on the client, and the message names the replacement method client. The obsolete methods are
///     warnings, not errors: they keep calling REST during their REST window. The reads, the rollup repair, the tenant
///     switches and the row writes stay without the attribute.
/// </summary>
public class StreamDataRestMethodsObsoleteTests
{
#pragma warning disable CS0618 // nameof of the obsolete members under test
    [Theory]
    [InlineData(nameof(IStreamDataServicesClient.ActivateArchiveAsync), "ActivateAsync of the archive's method client")]
    [InlineData(nameof(IStreamDataServicesClient.DisableArchiveAsync), "DisableAsync of the archive's method client")]
    [InlineData(nameof(IStreamDataServicesClient.EnableArchiveAsync), "EnableAsync of the archive's method client")]
    [InlineData(nameof(IStreamDataServicesClient.RetryArchiveActivationAsync), "RetryActivationAsync of the archive's method client")]
    [InlineData(nameof(IStreamDataServicesClient.DeleteArchiveAsync), "DeleteArchiveAsync of the archive's method client")]
    [InlineData(nameof(IStreamDataServicesClient.FreezeRollupArchiveAsync), "RtRollupArchiveMethodClient.FreezeAsync")]
    [InlineData(nameof(IStreamDataServicesClient.UnfreezeRollupArchiveAsync), "RtRollupArchiveMethodClient.UnfreezeAsync")]
    [InlineData(nameof(IStreamDataServicesClient.RewindRollupWatermarkAsync), "RtRollupArchiveMethodClient.RewindWatermarkAsync")]
    [InlineData(nameof(IStreamDataServicesClient.RecomputeArchiveAsync), "RtRollupArchiveMethodClient.Recompute2Async")]
    [InlineData(nameof(IStreamDataServicesClient.BackfillRollupFromSourceAsync), "RtRollupArchiveMethodClient.BackfillFromSource2Async")]
    [InlineData(nameof(IStreamDataServicesClient.AddArchiveColumnAsync), "AddColumnAsync of the archive's method client")]
    [InlineData(nameof(IStreamDataServicesClient.AddComputedColumnAsync), "AddComputedColumnAsync of the archive's method client")]
    [InlineData(nameof(IStreamDataServicesClient.RemoveComputedColumnAsync), "RemoveComputedColumnAsync of the archive's method client")]
    [InlineData(nameof(IStreamDataServicesClient.UpdateComputedColumnFormulaAsync), "UpdateComputedColumnFormulaAsync of the archive's method client")]
    [InlineData(nameof(IStreamDataServicesClient.RetryComputedColumnBackfillAsync), "RetryComputedColumnBackfillAsync of the archive's method client")]
#pragma warning restore CS0618
    public void MigratedRestMethod_IsObsolete_WithTheReplacement(string method, string replacement)
    {
        foreach (var type in new[] { typeof(IStreamDataServicesClient), typeof(StreamDataServicesClient) })
        {
            var obsolete = type.GetMethod(method)!.GetCustomAttribute<ObsoleteAttribute>();

            Assert.NotNull(obsolete);
            Assert.False(obsolete.IsError, "the REST method keeps working during its REST window");
            Assert.StartsWith($"Use {replacement}", obsolete.Message);
            Assert.Contains("generated from System.StreamData-[1.30,2.0)", obsolete.Message);
            Assert.Contains("method-client-migration-guide.md", obsolete.Message);
            if (replacement.EndsWith("of the archive's method client", StringComparison.Ordinal))
            {
                // The archive methods live on the client of the archive's concrete type: all three are named.
                Assert.Contains("RtRawArchiveMethodClient", obsolete.Message);
                Assert.Contains("RtTimeRangeArchiveMethodClient", obsolete.Message);
                Assert.Contains("RtRollupArchiveMethodClient", obsolete.Message);
            }
        }
    }

    [Theory]
    [InlineData(nameof(IStreamDataServicesClient.RecomputeArchiveAsync))]
    [InlineData(nameof(IStreamDataServicesClient.BackfillRollupFromSourceAsync))]
    public void AsyncReplacement_NamesTheOperationWait(string method)
    {
        foreach (var type in new[] { typeof(IStreamDataServicesClient), typeof(StreamDataServicesClient) })
        {
            var obsolete = type.GetMethod(method)!.GetCustomAttribute<ObsoleteAttribute>();

            Assert.NotNull(obsolete);
            Assert.Contains("CkOperationsClient.WaitForOperationAsync", obsolete.Message);
        }
    }

    [Theory]
    [InlineData(typeof(IStreamDataServicesClient))]
    [InlineData(typeof(StreamDataServicesClient))]
    public void OnlyTheMigratedOperations_AreObsolete(Type type)
    {
        // DeclaredOnly: the client's own members, not those of its base class.
        var obsolete = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttribute<ObsoleteAttribute>() != null)
            .Select(m => m.Name)
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.Equal([
            "ActivateArchiveAsync", "AddArchiveColumnAsync", "AddComputedColumnAsync", "BackfillRollupFromSourceAsync",
            "DeleteArchiveAsync", "DisableArchiveAsync", "EnableArchiveAsync", "FreezeRollupArchiveAsync",
            "RecomputeArchiveAsync", "RemoveComputedColumnAsync", "RetryArchiveActivationAsync",
            "RetryComputedColumnBackfillAsync", "RewindRollupWatermarkAsync", "UnfreezeRollupArchiveAsync",
            "UpdateComputedColumnFormulaAsync"
        ], obsolete);
    }
}
