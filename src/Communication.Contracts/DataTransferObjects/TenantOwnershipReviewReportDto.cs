namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Report of <c>POST system/v1/tenants/review-ownership</c> (AB#8796): this instance's tenant databases whose
///     ownership on the shared MongoDB server is not unambiguous.
/// </summary>
public class TenantOwnershipReviewReportDto
{
    /// <summary>
    ///     The system database (identity) of the instance that ran the review.
    /// </summary>
    public string? SystemDatabaseName { get; set; }

    /// <summary>
    ///     True when the review ran with stamp; false for a read-only listing.
    /// </summary>
    public bool Stamp { get; set; }

    /// <summary>
    ///     Tenant databases checked.
    /// </summary>
    public int Checked { get; set; }

    /// <summary>
    ///     Databases owned unambiguously by this instance (not listed).
    /// </summary>
    public int Owned { get; set; }

    /// <summary>
    ///     Registry rows whose database does not exist (skipped, never stamped).
    /// </summary>
    public int MissingDatabases { get; set; }

    /// <summary>
    ///     The listed databases.
    /// </summary>
    public List<TenantOwnershipReviewEntryDto> Entries { get; set; } = [];

    /// <summary>
    ///     One-line summary.
    /// </summary>
    public string? Summary { get; set; }
}

/// <summary>
///     A tenant database listed by the ownership review.
/// </summary>
public class TenantOwnershipReviewEntryDto
{
    /// <summary>
    ///     Tenant id in this instance.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    ///     Tenant database name.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    ///     <c>Unmarked</c> (no marker, registered only here), <c>MarkedByOtherInstance</c> or
    ///     <c>RegisteredByOtherInstance</c> (another system database on the server registers it as well).
    /// </summary>
    public string? Issue { get; set; }

    /// <summary>
    ///     The system database the ownership marker names, or null without marker.
    /// </summary>
    public string? MarkerOwnerSystemDatabaseName { get; set; }

    /// <summary>
    ///     All system databases on the server that register the database.
    /// </summary>
    public List<string> RegisteringSystemDatabases { get; set; } = [];

    /// <summary>
    ///     True when this call wrote the marker of this instance.
    /// </summary>
    public bool Stamped { get; set; }
}
