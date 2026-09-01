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
    /// Constructor
    /// </summary>
    /// <param name="originCkTypeId">Origin type id of the deep graph search starting point</param>
    /// <param name="originRtIds">Origin runtime id of the deep graph search starting point</param>
    public ExportModelRequestByDeepGraphDto(RtCkId<CkTypeId> originCkTypeId, IEnumerable<OctoObjectId> originRtIds)
    {
        OriginCkTypeId = originCkTypeId;
        OriginRtIds = originRtIds;
    }

    /// <summary>
    ///     The runtime IDs as starting point of the deep graph export.
    /// </summary>
    [JsonConverter(typeof(OctoObjectIdEnumerableConverter))]
    public IEnumerable<OctoObjectId> OriginRtIds { get; set; }

    /// <summary>
    ///     The CK type ID as starting point of the deep graph export.
    /// </summary>
    [JsonConverter(typeof(RtCkIdTypeIdConverter))]
    public RtCkId<CkTypeId> OriginCkTypeId { get; set; }

    /// <summary>
    ///     Optional directed follow rules the deep-graph traversal applies (AB#5003). Null or empty
    ///     keeps the default ParentChild traversal.
    /// </summary>
    public IEnumerable<DeepGraphFollowSpecDto>? FollowSpecs { get; set; }
}