namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.StreamData;

/// <summary>
/// One source archive a rollup aggregates from, together with the validity span over which that
/// source is authoritative (AB#5157). A rollup may declare several time-disjoint sources; the
/// spans of a normalised list never overlap. Mirrors the asset-repo REST payload exactly so the
/// SDK layer stays a thin transport.
/// </summary>
/// <param name="SourceArchiveRtId">Runtime id of the source archive (string form of <c>OctoObjectId</c>).</param>
/// <param name="ValidFrom">Inclusive start of the span this source covers; <c>null</c> means unbounded to the past.</param>
/// <param name="ValidTo">Exclusive end of the span this source covers; <c>null</c> means unbounded to the future.</param>
/// <remarks>Both bounds <c>null</c> describes a single, fully unbounded source.</remarks>
public sealed record RollupSourceReferenceDto(
    string SourceArchiveRtId,
    DateTime? ValidFrom,
    DateTime? ValidTo);
