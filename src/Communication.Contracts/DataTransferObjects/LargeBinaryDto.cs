using System.Text.Json.Serialization;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.ConstructionKit.Contracts.Serialization;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Large binary info
/// </summary>
public class LargeBinaryInfoDto
{
    /// <summary>
    ///     Gets or sets the binary id
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonConverter(typeof(OctoObjectIdConverter))]
    public OctoObjectId? BinaryId { get; set; }

    /// <summary>
    ///     Gets or sets the content type
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    ///     Gets or sets the filename
    /// </summary>
    public string? Filename { get; set; }

    /// <summary>
    ///     Gets or sets the upload date time
    /// </summary>
    public DateTime UploadDateTime { get; set; }

    /// <summary>
    ///     Gets or sets the length of binary
    /// </summary>
    public long? Size { get; set; }

    /// <summary>
    ///     Gets or sets the download uri
    /// </summary>
    public Uri? DownloadUri { get; set; }
}