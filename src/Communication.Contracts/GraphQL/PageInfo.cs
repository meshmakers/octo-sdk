namespace Meshmakers.Octo.Communication.Contracts.GraphQL;

/// <summary>
///     Represents a page info object.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class PageInfo
{
    /// <summary>
    ///     Returns the end cursor of the previous page or null if there is no previous page.
    /// </summary>
    public string? EndCursor { get; set; }

    /// <summary>
    ///     Returns true if there is a next page.
    /// </summary>
    public bool HasNextPage { get; set; }

    /// <summary>
    ///     Returns true if there is a previous page.
    /// </summary>
    public bool HasPreviousPage { get; set; }

    /// <summary>
    ///     Returns the start cursor of the next page or null if there is no next page.
    /// </summary>
    public string? StartCursor { get; set; }
}