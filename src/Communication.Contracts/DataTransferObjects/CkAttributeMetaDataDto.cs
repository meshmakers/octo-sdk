namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Represents construction kit attribute meta data
/// </summary>
public class CkAttributeMetaDataDto
{
    /// <summary>
    ///     Metadata key
    /// </summary>
    public string Key { get; set; } = null!;

    /// <summary>
    ///     Metadata value
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    ///     An optional description of the meta data
    /// </summary>
    public string? Description { get; set; }
}