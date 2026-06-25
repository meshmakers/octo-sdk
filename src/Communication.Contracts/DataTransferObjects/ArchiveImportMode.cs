namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Import mode for archive row data. Mirrors the asset-repo <c>ImportMode</c> and is serialized as a
/// string on the wire (e.g. <c>?mode=Upsert</c>). Archive data export/import concept (AB#4230) §4.1 / §7.
/// </summary>
public enum ArchiveImportMode
{
    /// <summary>
    /// Only inserts rows; conflicts on the natural key are an error. Default for raw archives.
    /// </summary>
    InsertOnly = 0,

    /// <summary>
    /// Inserts or updates rows on natural-key conflict (<c>ON CONFLICT</c> upsert). Required for
    /// windowed (time-range / rollup) archives.
    /// </summary>
    Upsert = 1,
}
