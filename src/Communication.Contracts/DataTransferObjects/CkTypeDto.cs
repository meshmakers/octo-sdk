using System.Diagnostics;
using Meshmakers.Octo.Communication.Contracts.GraphQL;
using Meshmakers.Octo.ConstructionKit.Contracts;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Represents a type in the construction kit.
/// </summary>
[DebuggerDisplay("{" + nameof(CkTypeId) + "}")]
public class CkTypeDto
{
    /// <summary>
    /// Gets or sets the construction kit id, e.g. System-1.0.0/Entity-2
    /// </summary>
    public CkId<CkTypeId> CkTypeId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the runtime construction kit id, that has no versioning information for model id,
    /// but for the element id - e.g. System/Entity-2
    /// </summary>
    public RtCkId<CkTypeId> RtCkTypeId { get; set; } = null!;

    /// <summary>
    ///     An optional description of the type
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     If true, the type cannot be inherited again
    /// </summary>
    public bool IsFinal { get; set; }

    /// <summary>
    ///     If true, the type cannot be instantiated by a runtime entity
    /// </summary>
    public bool IsAbstract { get; set; }

    /// <summary>
    ///     Get or sets a connection to attributes
    /// </summary>
    public Connection<CkTypeAttributeDto>? Attributes { get; set; }

    /// <summary>
    ///     Get or sets a connection to base types
    /// </summary>
    public Connection<CkTypeDto>? BaseType { get; set; }

    /// <summary>
    ///     Get or sets a connection to derived types
    /// </summary>
    public Connection<CkTypeDto>? DerivedTypes { get; set; }
}