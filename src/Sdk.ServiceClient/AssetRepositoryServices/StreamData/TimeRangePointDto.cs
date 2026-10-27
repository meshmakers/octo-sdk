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
