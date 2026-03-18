using Meshmakers.Octo.ConstructionKit.Contracts;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Data Transfer Object for an external tenant user mapping.
/// </summary>
public record ExternalTenantUserMappingDto
{
    /// <summary>
    ///     Unique ID for the mapping.
    /// </summary>
    public OctoObjectId? Id { get; init; }

    /// <summary>
    ///     The source tenant ID.
    /// </summary>
    public string SourceTenantId { get; init; } = string.Empty;

    /// <summary>
    ///     The source user ID.
    /// </summary>
    public string SourceUserId { get; init; } = string.Empty;

    /// <summary>
    ///     The source user name.
    /// </summary>
    public string SourceUserName { get; init; } = string.Empty;

    /// <summary>
    ///     Role IDs assigned to this mapping.
    /// </summary>
    public List<string> RoleIds { get; init; } = [];

    /// <summary>
    ///     Group names the user is a member of.
    /// </summary>
    public List<string> GroupNames { get; init; } = [];
}
