namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Result containing groups.
/// </summary>
public class GroupsResult
{
    /// <summary>
    ///     The groups.
    /// </summary>
    public IEnumerable<GroupDto> Groups { get; set; } = [];
}
