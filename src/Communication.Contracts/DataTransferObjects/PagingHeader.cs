namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Represents a paging header
/// </summary>
public class PagingHeader
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="totalCount">Total count of items based on query</param>
    /// <param name="skip">Amount of items skipped</param>
    /// <param name="take">Amount of items taken</param>
    public PagingHeader(
        long totalCount, int skip, int take)
    {
        TotalCount = totalCount;
        Skip = skip;
        Take = take;
    }

    /// <summary>
    /// Returns the total count of items available
    /// </summary>
    public long TotalCount { get; }
    
    /// <summary>
    /// Returns the amount of items skipped
    /// </summary>
    public int Skip { get; }
    
    /// <summary>
    /// Returns the amount of items taken
    /// </summary>
    public int Take { get; }
}