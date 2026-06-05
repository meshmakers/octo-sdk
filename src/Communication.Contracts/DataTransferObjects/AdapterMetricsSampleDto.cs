using Meshmakers.Octo.ConstructionKit.Contracts;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Resource-utilisation snapshot pushed periodically from an adapter process to the
/// communication controller. The controller keeps the most recent samples in an
/// in-memory ring buffer so the UI can render live CPU / memory sparklines.
/// </summary>
public record AdapterMetricsSampleDto
{
    /// <summary>
    /// Identifier of the reporting adapter.
    /// </summary>
    public required RtEntityId AdapterRtEntityId { get; init; }

    /// <summary>
    /// UTC timestamp the sample was captured at on the adapter side.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// CPU utilisation in percent (0..100), normalised across all available cores
    /// — 100% means every core was fully saturated during the sampling window.
    /// </summary>
    public required double CpuPercent { get; init; }

    /// <summary>
    /// Working set of the adapter process in bytes (Process.WorkingSet64).
    /// </summary>
    public required long WorkingSetBytes { get; init; }

    /// <summary>
    /// Managed-heap size reported by the GC in bytes (GC.GetTotalMemory(false)).
    /// </summary>
    public required long GcHeapBytes { get; init; }

    /// <summary>
    /// Total thread count of the adapter process.
    /// </summary>
    public required int ThreadCount { get; init; }
}
