namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.StreamData;

/// <summary>
/// Whether a stream data value was entered manually or delivered automatically (AB#5356). Sent on the wire
/// as <c>manual</c> / <c>automated</c>.
/// </summary>
public enum StreamDataOrigin
{
    /// <summary>Delivered by a system: an adapter, a pipeline, an import. The default.</summary>
    Automated = 0,

    /// <summary>Entered or corrected by a person.</summary>
    Manual = 1
}

/// <summary>
/// One externally aggregated time-range value for <see cref="IStreamDataServicesClient.InsertTimeRangeAsync" />,
/// covering the half-open window <c>[From, To)</c>. Mirrors the asset-repo REST payload of
/// <c>POST streamdata/archives/{archiveRtId}/insertTimeRange</c>.
/// </summary>
public sealed record TimeRangePointDto
{
    /// <summary>Runtime id of the entity the value belongs to.</summary>
    public required string RtId { get; init; }

    /// <summary>Unversioned CK type id of the entity, e.g. <c>Basic.Energy/EnergyMeasurement</c>.</summary>
    public required string CkTypeId { get; init; }

    /// <summary>Inclusive window start (UTC).</summary>
    public required DateTime From { get; init; }

    /// <summary>Exclusive window end (UTC).</summary>
    public required DateTime To { get; init; }

    /// <summary>Optional human-readable name of the entity.</summary>
    public string? RtWellKnownName { get; init; }

    /// <summary>User-column values keyed by attribute path (e.g. <c>Amount.Value</c>).</summary>
    public IReadOnlyDictionary<string, object?> Attributes { get; init; } = new Dictionary<string, object?>();

    /// <summary>
    /// Whether the value was entered manually or delivered automatically (AB#7596). Defaults to
    /// <see cref="StreamDataOrigin.Automated" />. The service records the writer of a manual value from the
    /// caller's token.
    /// </summary>
    public StreamDataOrigin Origin { get; init; } = StreamDataOrigin.Automated;

    /// <summary>Optional reason of a manual write (AB#7593); only allowed with <see cref="StreamDataOrigin.Manual" />.</summary>
    public string? Reason { get; init; }
}

/// <summary>
/// What a stream data write stored (AB#7598): mirrors the asset-repo response of
/// <c>POST streamdata/archives/{archiveRtId}/insertTimeRange</c>.
/// </summary>
public sealed record StreamDataWriteResultDto
{
    /// <summary>Rows the archive holds after the write (new, won against the stored row, or identical).</summary>
    public int Stored { get; init; }

    /// <summary>Rows that were not stored.</summary>
    public int Skipped { get; init; }

    /// <summary>
    /// Rows not stored per reason: <c>LowerPrecedence</c> (the archive's conflict precedence ranks the stored row
    /// above), <c>ManualValueProtected</c> (a manual value is never replaced by an automated delivery),
    /// <c>CkTypeMismatch</c> (not the archive's CK type).
    /// </summary>
    public IReadOnlyDictionary<string, int> SkippedByReason { get; init; } = new Dictionary<string, int>();

    /// <summary>The rows not stored (at most the first 1,000; <see cref="Skipped" /> is the full count).</summary>
    public IReadOnlyList<StreamDataSkippedRowDto> SkippedRows { get; init; } = [];
}

/// <summary>One row of a write that was not stored (AB#7598).</summary>
public sealed record StreamDataSkippedRowDto
{
    /// <summary>Entity the row belongs to.</summary>
    public string RtId { get; init; } = string.Empty;

    /// <summary>Window start (or timestamp of a raw row), UTC.</summary>
    public DateTime From { get; init; }

    /// <summary>Window end, UTC; null for a raw row.</summary>
    public DateTime? To { get; init; }

    /// <summary><c>manual</c> or <c>automated</c>, as the writer declared it.</summary>
    public string Origin { get; init; } = "automated";

    /// <summary>Why the row was not stored; see <see cref="StreamDataWriteResultDto.SkippedByReason" />.</summary>
    public string Reason { get; init; } = string.Empty;
}
