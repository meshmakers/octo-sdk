namespace Meshmakers.Octo.Communication.Contracts.GraphQL;

/// <summary>
///     Represents an edge object.
/// </summary>
/// <typeparam name="TDto"></typeparam>
public class Edge<TDto>
{
    /// <summary>
    ///     Gets or sets the cursor.
    /// </summary>
    public string? Cursor { get; set; }

    /// <summary>
    ///     Gets or sets the node.
    /// </summary>
    public TDto? Node { get; set; }
}