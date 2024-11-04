namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Represents a query cell in a query result
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class RtQueryCellDto : GraphQlDto
{
    /// <summary>
    ///     Gets or sets the attribute path.
    /// </summary>
    public required string AttributePath { get; set; }

    /// <summary>
    ///     Gets or sets the attribute value.
    /// </summary>
    public object? Value { get; set; }
}