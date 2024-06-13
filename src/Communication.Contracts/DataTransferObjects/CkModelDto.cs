using System.Diagnostics;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Represents a construction kit model
/// </summary>
[DebuggerDisplay("{" + nameof(Id) + "}")]
public class CkModelDto
{
    /// <summary>
    ///     Defines the id of the construction kit model
    /// </summary>
    public CkModelId Id { get; set; } = null!;
    
    /// <summary>
    ///     An optional description of the model
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Defines the state of the construction kit model
    /// </summary>
    public ModelState ModelState { get; set; }
}