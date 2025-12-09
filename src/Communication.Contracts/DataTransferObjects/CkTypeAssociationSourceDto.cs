using Meshmakers.Octo.ConstructionKit.Contracts;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// DTO to return all, inherited only
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class CkTypeAssociationSourceDto
{
    /// <summary>
    /// Gets or sets the type id of the construction kit type we are getting associations for
    /// </summary>
    public required CkId<CkTypeId> CkTypeId { get; set; }

    /// <summary>
    /// Gets or sets the direction of the associations
    /// </summary>
    public required GraphDirections Direction { get; set; }

    /// <summary>
    /// Gets or sets all associations definitions available current type
    /// </summary>
    public IEnumerable<CkTypeAssociationDto>? All { get; set; }

    /// <summary>
    /// Gets or sets associations definitions inherited by base types
    /// </summary>
    public IEnumerable<CkTypeAssociationDto>? Inherited { get; set; }

    /// <summary>
    /// Gets or sets associations definitions defined by the current type
    /// </summary>
    public IEnumerable<CkTypeAssociationDto>? Owned { get; set; }
}