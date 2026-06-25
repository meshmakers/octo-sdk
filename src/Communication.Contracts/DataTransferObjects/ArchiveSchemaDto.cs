namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// SDK projection of an archive's schema as returned by the asset-repo
/// <c>archives/{archiveRtId}/schema</c> endpoint. Mirrors the <c>metadata.archive</c> block of the
/// export ZIP exactly (see concept §3.1) so the SDK layer is a thin transport. Consumed by the bot
/// export/import jobs for pre-flight schema-match validation (§6). Archive data export/import
/// concept (AB#4230).
/// </summary>
/// <param name="RtId">Runtime id of the <c>CkArchive</c> entity (string form of <c>OctoObjectId</c>).</param>
/// <param name="RtWellKnownName">Optional well-known name of the archive.</param>
/// <param name="Kind">Archive kind — <c>raw</c> / <c>timeRange</c> / <c>rollup</c>.</param>
/// <param name="TargetCkTypeId">The archived CK type id (== <c>rtCkTypeId</c>); import match key #1.</param>
/// <param name="Columns">The user-configured columns; import match key #2.</param>
/// <param name="RollupAggregations">Aggregation specs; populated only when <see cref="Kind"/> is <c>rollup</c>.</param>
/// <param name="PeriodMs">Advisory window period in milliseconds; populated only when <see cref="Kind"/> is <c>timeRange</c>. Wire form matches the asset-repo <c>periodMs</c> field (portable numeric, not a .NET TimeSpan string).</param>
public sealed record ArchiveSchemaDto(
    string RtId,
    string? RtWellKnownName,
    string Kind,
    string TargetCkTypeId,
    IReadOnlyList<ArchiveColumnDto> Columns,
    IReadOnlyList<ArchiveRollupAggregationDto>? RollupAggregations,
    long? PeriodMs);
