// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Gets or sets the global query options.
/// </summary>
public class GlobalQueryOptionsDto
{
    /// <summary>
    ///     Gets or sets an optional flag to include archived entities in the query results.
    /// </summary>
    public bool? IncludeArchivedEntities { get; set; }
}