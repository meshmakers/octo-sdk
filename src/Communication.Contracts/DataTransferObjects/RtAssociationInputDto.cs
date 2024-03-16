using System.Text.Json.Serialization;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Runtime.Contracts;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Represents a runtime association for input
/// </summary>
public class RtAssociationInputDto
{
    /// <summary>
    ///     The target entity of the association
    /// </summary>
    public RtEntityIdDto Target { get; set; } = null!;

    /// <summary>
    ///     Type of mod operation
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AssociationModOptionsDto? ModOption { get; set; }
}