// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Gets or sets a search filter.
/// </summary>
public class SearchFilterDto
{
    /// <summary>
    ///     Gets or sets the search filter types.
    /// </summary>
    public SearchFilterTypesDto? Type { get; set; }

    /// <summary>
    ///     Gets or sets the language
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    ///     Gets or sets the search term.
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    ///     Gets or sets the attribute names.
    /// </summary>
    public string[]? AttributeNames { get; set; }
}