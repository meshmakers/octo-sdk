using System.Diagnostics;
using System.Text.Json.Serialization;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Represents a role of an association between two ck types.
/// </summary>
[DebuggerDisplay("{" + nameof(CkAssociationRoleId) + "}")]
public class CkAssociationRoleDto
{
    /// <summary>
    ///     Gets or sets the id of the association role
    /// </summary>
    [JsonRequired]
    public CkId<CkAssociationRoleId> CkAssociationRoleId { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the name of the association for inbound references (e.g. Children)
    /// </summary>
    [JsonRequired]
    public string InboundName { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the name of the association for outbound references (e.g. Parent)
    /// </summary>
    [JsonRequired]
    public string OutboundName { get; set; } = null!;
    
    /// <summary>
    ///     An optional description of the role
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Multiplicity of the inbound association
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [JsonRequired]
    public MultiplicitiesDto InboundMultiplicity { get; set; }

    /// <summary>
    ///     Multiplicity of the outbound association
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [JsonRequired]
    public MultiplicitiesDto OutboundMultiplicity { get; set; }
}