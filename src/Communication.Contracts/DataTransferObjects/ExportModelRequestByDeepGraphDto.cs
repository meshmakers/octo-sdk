using System.Text.Json.Serialization;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.ConstructionKit.Contracts.Serialization;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Request DTO for exporting a runtime model using deep graph.
/// </summary>
public class ExportModelRequestByDeepGraphDto
{
    /// <summary>
    ///     The runtime IDs as starting point of the deep graph export.
    /// </summary>
    [JsonConverter(typeof(OctoObjectIdArrayConverter))]
    public IEnumerable<OctoObjectId>? OriginRtIds { get; set; }
    
    /// <summary>
    ///     The CK type ID as starting point of the deep graph export.
    /// </summary>
    public CkId<CkTypeId>? OriginCkTypeId { get; set; }
}