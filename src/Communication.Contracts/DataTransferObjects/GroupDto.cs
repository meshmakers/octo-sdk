using Meshmakers.Octo.ConstructionKit.Contracts;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Data Transfer Object for a group.
/// </summary>
public record GroupDto
{
    /// <summary>
    ///     Unique ID for the group.
    /// </summary>
    public OctoObjectId? Id { get; init; }

    /// <summary>
    ///     Name of the group.
    /// </summary>
    public string GroupName { get; init; } = string.Empty;

    /// <summary>
    ///     Optional description of the group.
    /// </summary>
    public string? GroupDescription { get; init; }

    /// <summary>
    ///     Role IDs assigned to this group.
    /// </summary>
    public List<string> RoleIds { get; init; } = [];

    /// <summary>
    ///     User IDs that are members of this group.
    /// </summary>
    public List<string> MemberUserIds { get; init; } = [];

    /// <summary>
    ///     External user IDs that are members of this group.
    /// </summary>
    public List<string> MemberExternalUserIds { get; init; } = [];

    /// <summary>
    ///     Client IDs that are members of this group (AB#4183).
    /// </summary>
    public List<string> MemberClientIds { get; init; } = [];

    /// <summary>
    ///     Child group IDs that are members of this group.
    /// </summary>
    public List<string> MemberGroupIds { get; init; } = [];
}
