

// ReSharper disable MemberCanBePrivate.Global

namespace Meshmakers.Octo.Common.Shared.DataTransferObjects;

/// <summary>
/// Represents a result set that is paged.
/// </summary>
/// <typeparam name="T"></typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="source">The source of data as list</param>
    /// <param name="totalCount">Total count of items based on query</param>
    /// <param name="skip">Amount of items skipped</param>
    /// <param name="take">Amount of items taken</param>
    public PagedResult(IEnumerable<T> source, int? skip, int? take, long totalCount)
    {
        TotalCount = totalCount;
        Skip = skip;
        Take = take;
        List = source.ToList();
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="source">The source of data as list</param>
    public PagedResult(IEnumerable<T> source)
    {
        List = source.ToList();
        TotalCount = List.Count();
    }

    /// <summary>
    /// Returns the total count of items available
    /// </summary>
    public long TotalCount { get; }
    
    /// <summary>
    /// Returns the amount of items skipped
    /// </summary>
    public int? Skip { get; }
    
    /// <summary>
    /// Returns the amount of items taken
    /// </summary>
    public int? Take { get; }
    
    /// <summary>
    /// Returns the paged result set
    /// </summary>
    public ICollection<T> List { get; }

    /// <summary>
    /// Creates a paging header
    /// </summary>
    /// <returns></returns>
    public PagingHeader? GetHeader()
    {
        if (Skip.HasValue && Take.HasValue)
            return new PagingHeader(
                TotalCount, Skip.Value,
                Take.Value);

        return null;
    }
}