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

    /// <summary>
    ///     True when the tenant is a scratch tenant (AB#7192): created with the <c>scratch</c> flag
    ///     by a client-credentials client of its parent, which then works in it as the child's own administrator.
    ///     Only populated by the endpoints that list or read child tenants.
    /// </summary>
    public bool IsScratch { get; set; }

    /// <summary>Client id of the scratch tenant's creator; null for a tenant that is not a scratch tenant.</summary>
    public string? ScratchCreatorClientId { get; set; }

    /// <summary>When the scratch tenant was created (UTC); null for a tenant that is not a scratch tenant.</summary>
    public DateTime? ScratchCreatedAtUtc { get; set; }

    /// <summary>
    ///     True when the creator's access to its scratch tenant was revoked. Identity does not grant it again.
    /// </summary>
    public bool ScratchAccessRevoked { get; set; }
}
