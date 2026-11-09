namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.StreamData;

/// <summary>
/// One value of a raw archive for <see cref="IStreamDataServicesClient.InsertAsync" /> (AB#8590). Mirrors the asset-repo
/// REST payload of <c>POST streamdata/archives/{archiveRtId}/insert</c>.
/// </summary>
/// <remarks>
/// A raw row is identified by archive + <see cref="RtId" /> + <see cref="Timestamp" /> (UTC, millisecond precision); the
/// CK type is fixed per archive. Writing the same identity again corrects the row, subject to the archive's conflict
/// precedence: a manual row is never replaced by an automated delivery, and the protection covers the whole row.
/// </remarks>
public sealed record RawPointDto
{
    /// <summary>Runtime id of the entity the value belongs to.</summary>
    public required string RtId { get; init; }

    /// <summary>Unversioned CK type id of the entity, e.g. <c>Basic.Energy/EnergyMeasurement</c>.</summary>
    public required string CkTypeId { get; init; }

    /// <summary>
    /// Time of the value (UTC); an unspecified kind is sent as UTC. The service refuses sub-millisecond precision.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>Optional human-readable name of the entity.</summary>
    public string? RtWellKnownName { get; init; }

    /// <summary>
    /// Ingested column values keyed by attribute path (e.g. <c>Amount.Value</c>). A manual point must name every ingested
    /// column of the archive; <c>null</c> is a valid value.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Attributes { get; init; } = new Dictionary<string, object?>();

    /// <summary>
    /// Whether the value was entered manually or delivered automatically. Defaults to
    /// <see cref="StreamDataOrigin.Automated" />. The service records the writer of a manual value from the caller's token.
    /// </summary>
    public StreamDataOrigin Origin { get; init; } = StreamDataOrigin.Automated;

    /// <summary>Optional reason of a manual write; only allowed with <see cref="StreamDataOrigin.Manual" />.</summary>
    public string? Reason { get; init; }
}
