// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Paging parameters.
/// </summary>
public class PagingParams
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PagingParams"/> class.
    /// </summary>
    public PagingParams()
    {
        Skip = 0;
        Take = 100;
    }

    /// <summary>
    /// Returns the amount of items skipped
    /// </summary>
    public int Skip { get; set; }
    
    /// <summary>
    /// Returns the amount of items taken
    /// </summary>
    public int Take { get; set; }

    /// <summary>
    /// Returns an optional filter expression.
    /// </summary>
    public string? Filter { get; set; }
}