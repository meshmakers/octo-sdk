namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// SDK projection of one rollup aggregation spec. Mirrors the asset-repo REST payload
/// (<c>CkRollupAggregationSpec</c>) so the SDK layer is a thin transport. Populated only when the
/// archive kind is <c>rollup</c>. Used as an import schema-match key for rollup archives. Archive
/// data export/import concept (AB#4230) §6.
/// </summary>
/// <param name="SourcePath">Source column path the aggregation is computed from.</param>
/// <param name="Function">Aggregation function name (e.g. <c>avg</c>, <c>sum</c>); serialized as string.</param>
/// <param name="TargetColumnName">Optional derived target column name; null lets the engine derive it.</param>
public sealed record ArchiveRollupAggregationDto(
    string SourcePath,
    string Function,
    string? TargetColumnName);
