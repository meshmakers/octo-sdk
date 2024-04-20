using System.Text.Json.Serialization;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.ConstructionKit.Contracts.Serialization;
using Meshmakers.Octo.Runtime.Contracts.Serialization;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Represents a unique identifier of a runtime model entity and its construction kit type.
/// </summary>
public class RtEntityIdDto
{
    /// <summary>
    ///     Returns the runtime id.
    /// </summary>
    [JsonConverter(typeof(OctoObjectIdConverter))]
    [Newtonsoft.Json.JsonConverter(typeof(NewtonOctoObjectIdConverter))]
    public OctoObjectId RtId { get; set; }

    /// <summary>
    ///     The construction kit type id.
    /// </summary>
    [JsonConverter(typeof(CkIdTypeIdConverter))]
    public CkId<CkTypeId> CkTypeId { get; set; } = null!;
}