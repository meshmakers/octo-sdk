using System.Text.Json.Serialization;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.ConstructionKit.Contracts.Serialization;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Request DTO for exporting a model.
/// </summary>
public class ExportModelRequestDto
{
    /// <summary>
    ///     The query ID.
    /// </summary>
    [JsonConverter(typeof(OctoObjectIdConverter))]
    public OctoObjectId QueryId { get; set; }
}