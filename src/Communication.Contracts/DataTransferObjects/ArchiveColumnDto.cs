namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// SDK projection of one configured archive column. Mirrors the asset-repo REST payload
/// (<c>CkArchiveColumnSpec</c>) exactly so the SDK layer is a thin transport. Used as an import
/// schema-match key (path / indexed / required). Archive data export/import concept (AB#4230) §3.1.
/// </summary>
/// <param name="Path">User-configured column path into the target CK type.</param>
/// <param name="Indexed">Whether the column is indexed in the underlying store.</param>
/// <param name="Required">Whether the column is required.</param>
public sealed record ArchiveColumnDto(
    string Path,
    bool Indexed,
    bool Required);
