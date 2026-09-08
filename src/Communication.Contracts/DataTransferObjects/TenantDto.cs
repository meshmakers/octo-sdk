namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Data transfer object for Octo tenants
/// </summary>
public class TenantDto
{
    /// <summary>
    ///     Gets or sets the database name
    /// </summary>
    public string? Database { get; set; }

    /// <summary>
    ///     Gets or sets Id of tenant
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    ///     Gets or sets the id of the tenant's parent tenant. Populated by the
    ///     descendants listing (AB#5151), where the caller needs to see the tree;
    ///     the plain child listing leaves it null (every entry's parent is the
    ///     requesting tenant).
    /// </summary>
    public string? ParentTenantId { get; set; }
}