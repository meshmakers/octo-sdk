using System.Text.Json.Serialization;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.ConstructionKit.Contracts.Serialization;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Request DTO for exporting a runtime model using a query
/// </summary>
public class ExportModelRequestByQueryDto
{
    /// <summary>
    ///     The query ID.
    /// </summary>
    [JsonConverter(typeof(OctoObjectIdConverter))]
    public OctoObjectId QueryId { get; set; }
}