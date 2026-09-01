using Meshmakers.Octo.ConstructionKit.Contracts;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     One directed edge-following rule for the role-set deep-graph export (AB#5003): follow the
///     given association role only in the given direction. Directed following keeps hub types as
///     dead-ends so the export does not over-collect the connected graph.
/// </summary>
public class DeepGraphFollowSpecDto
{
    /// <summary>The association role id to follow, e.g. <c>System.Identity/PolicyPermission</c>.</summary>
    public required string RoleId { get; set; }

    /// <summary>
    ///     <see cref="GraphDirections.Outbound" /> follows origin&#8594;target,
    ///     <see cref="GraphDirections.Inbound" /> follows target&#8594;origin.
    /// </summary>
    public required GraphDirections Direction { get; set; }
}
