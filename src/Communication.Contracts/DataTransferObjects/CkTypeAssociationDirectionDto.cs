using Meshmakers.Octo.ConstructionKit.Contracts;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// DTO to return inbound and outbound association definitions
/// </summary>
public class CkTypeAssociationDirectionDto
{
    /// <summary>
    /// Gets or sets the type id of the construction kit type we are getting associations for
    /// </summary>
    public required CkId<CkTypeId> CkTypeId { get; set; }

    /// <summary>
    /// Gets or sets the ingoing associations
    /// </summary>
    public CkTypeAssociationSourceDto? In { get; set; }

    /// <summary>
    /// Get or sets the output associations
    /// </summary>
    public CkTypeAssociationSourceDto? Out { get; set; }
}