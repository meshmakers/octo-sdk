namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.StreamData;

/// <summary>
///     AB#9059: messages of the <see cref="global::System.ObsoleteAttribute" /> on the archive operations of
///     <see cref="IStreamDataServicesClient" /> that the CK methods of <c>System.StreamData</c> (1.24.0 - 1.30.0) replace.
///     The replacement is the method client a consumer generates from <c>System.StreamData-[1.30,2.0)</c>
///     (<c>construction-kits.yaml</c>, together with <c>System-[2.7,3.0)</c>); see
///     <c>docs/method-client-migration-guide.md</c>, "Stream-data archives". The REST methods keep working during their
///     REST window. An archive method is called on the client of the archive's concrete type
///     (<c>RtRawArchiveMethodClient</c>, <c>RtTimeRangeArchiveMethodClient</c> or <c>RtRollupArchiveMethodClient</c>).
/// </summary>
internal static class StreamDataMethodObsoleteMessages
{
    private const string Suffix =
        " generated from System.StreamData-[1.30,2.0) (construction-kits.yaml); the REST method keeps working during its REST window. See docs/method-client-migration-guide.md, Stream-data archives (AB#9059).";

    private const string ArchiveClients =
        " of the archive's method client (RtRawArchiveMethodClient, RtTimeRangeArchiveMethodClient or RtRollupArchiveMethodClient)";

    private const string OperationWait = ", then CkOperationsClient.WaitForOperationAsync with the returned handle,";

    /// <summary>Replacement of <see cref="IStreamDataServicesClient.ActivateArchiveAsync" />.</summary>
    public const string ActivateArchive = "Use ActivateAsync" + ArchiveClients + Suffix;

    /// <summary>Replacement of <see cref="IStreamDataServicesClient.DisableArchiveAsync" />.</summary>
    public const string DisableArchive = "Use DisableAsync" + ArchiveClients + Suffix;

    /// <summary>Replacement of <see cref="IStreamDataServicesClient.EnableArchiveAsync" />.</summary>
    public const string EnableArchive = "Use EnableAsync" + ArchiveClients + Suffix;

    /// <summary>Replacement of <see cref="IStreamDataServicesClient.RetryArchiveActivationAsync" />.</summary>
    public const string RetryArchiveActivation = "Use RetryActivationAsync" + ArchiveClients + Suffix;

    /// <summary>Replacement of <see cref="IStreamDataServicesClient.DeleteArchiveAsync" />.</summary>
    public const string DeleteArchive = "Use DeleteArchiveAsync" + ArchiveClients + Suffix;

    /// <summary>Replacement of <see cref="IStreamDataServicesClient.FreezeRollupArchiveAsync" />.</summary>
    public const string FreezeRollupArchive = "Use RtRollupArchiveMethodClient.FreezeAsync" + Suffix;

    /// <summary>Replacement of <see cref="IStreamDataServicesClient.UnfreezeRollupArchiveAsync" />.</summary>
    public const string UnfreezeRollupArchive = "Use RtRollupArchiveMethodClient.UnfreezeAsync" + Suffix;

    /// <summary>Replacement of <see cref="IStreamDataServicesClient.RewindRollupWatermarkAsync" />.</summary>
    public const string RewindRollupWatermark = "Use RtRollupArchiveMethodClient.RewindWatermarkAsync" + Suffix;

    /// <summary>Replacement of <see cref="IStreamDataServicesClient.RecomputeArchiveAsync" />.</summary>
    public const string RecomputeArchive = "Use RtRollupArchiveMethodClient.Recompute2Async" + OperationWait + Suffix;

    /// <summary>Replacement of <see cref="IStreamDataServicesClient.BackfillRollupFromSourceAsync" />.</summary>
    public const string BackfillRollupFromSource =
        "Use RtRollupArchiveMethodClient.BackfillFromSource2Async" + OperationWait + Suffix;

    /// <summary>Replacement of <see cref="IStreamDataServicesClient.AddArchiveColumnAsync" />.</summary>
    public const string AddArchiveColumn = "Use AddColumnAsync" + ArchiveClients + Suffix;

    /// <summary>Replacement of <see cref="IStreamDataServicesClient.AddComputedColumnAsync" />.</summary>
    public const string AddComputedColumn = "Use AddComputedColumnAsync" + ArchiveClients + Suffix;

    /// <summary>Replacement of <see cref="IStreamDataServicesClient.RemoveComputedColumnAsync" />.</summary>
    public const string RemoveComputedColumn = "Use RemoveComputedColumnAsync" + ArchiveClients + Suffix;

    /// <summary>Replacement of <see cref="IStreamDataServicesClient.UpdateComputedColumnFormulaAsync" />.</summary>
    public const string UpdateComputedColumnFormula = "Use UpdateComputedColumnFormulaAsync" + ArchiveClients + Suffix;

    /// <summary>Replacement of <see cref="IStreamDataServicesClient.RetryComputedColumnBackfillAsync" />.</summary>
    public const string RetryComputedColumnBackfill = "Use RetryComputedColumnBackfillAsync" + ArchiveClients + Suffix;
}
