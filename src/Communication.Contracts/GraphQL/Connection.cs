namespace Meshmakers.Octo.Communication.Contracts.GraphQL;

/// <summary>
/// Represents a connection object.
/// </summary>
/// <typeparam name="TDto"></typeparam>
// ReSharper disable once ClassNeverInstantiated.Global
public class Connection<TDto>
{
    /// <summary>
    /// Returns the edges.
    /// </summary>
    public ICollection<TDto>? Edges { get; set; }

    /// <summary>
    /// Returns the items.
    /// </summary>
    public ICollection<TDto>? Items { get; set; }

    /// <summary>
    /// Returns the page info.
    /// </summary>
    public PageInfo? PageInfo { get; set; }
    
    /// <summary>
    /// Returns the total count.
    /// </summary>
    public int TotalCount { get; set; }
}