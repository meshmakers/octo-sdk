using Meshmakers.Octo.ConstructionKit.Contracts;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Data Transfer Object for a data policy (AB#4972): binds a data permission to CK types with
///     actions, scope and enforcement mode.
/// </summary>
public record DataPolicyDto
{
    /// <summary>
    ///     Unique ID of the policy.
    /// </summary>
    public OctoObjectId? Id { get; init; }

    /// <summary>
    ///     CK type ids (or collection roots) the policy targets; derived types inherit.
    /// </summary>
    public List<string> TargetCkTypeIds { get; init; } = [];

    /// <summary>
    ///     Granted actions: Read, Write, Delete.
    /// </summary>
    public List<string> Actions { get; init; } = [];

    /// <summary>
    ///     "All" or "OwnedOnly" (restricted to entities created by the caller).
    /// </summary>
    public string Scope { get; init; } = "All";

    /// <summary>
    ///     "Enforce" or "AuditOnly" (violations only logged — migration mode).
    /// </summary>
    public string EnforcementMode { get; init; } = "Enforce";
}

/// <summary>
///     Data Transfer Object for a data permission (AB#4972) with its policies and role grants.
/// </summary>
public record DataPermissionDto
{
    /// <summary>
    ///     Unique ID of the permission.
    /// </summary>
    public OctoObjectId? Id { get; init; }

    /// <summary>
    ///     Dot-namespaced permission id, e.g. "accounting.documents".
    /// </summary>
    public string PermissionId { get; init; } = string.Empty;

    /// <summary>
    ///     Optional description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    ///     Names of the roles the permission is granted to.
    /// </summary>
    public List<string> GrantedRoleNames { get; init; } = [];

    /// <summary>
    ///     The policies bound to this permission.
    /// </summary>
    public List<DataPolicyDto> Policies { get; init; } = [];
}
