using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// DTO for associations defined at types
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class CkTypeAssociationDto
{
    /// <summary>
    /// Gets or sets the type id of the construction kit type of the origin side of the association
    /// </summary>
    public required CkId<CkTypeId> OriginCkTypeId { get; set; }

    /// <summary>
    /// Gets or sets the type id of the construction kit type of the target side of the association
    /// </summary>
    public required CkId<CkTypeId> TargetCkTypeId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property name of the association for the current side
    /// </summary>
    public required string NavigationPropertyName { get; set; }

    /// <summary>
    /// Gets or sets the multiplicity of the association for the current side
    /// </summary>
    public required MultiplicitiesDto Multiplicity { get; set; }

    /// <summary>
    /// Gets or sets the association role
    /// </summary>
    public required CkId<CkAssociationRoleId> RoleId { get; set; }
}