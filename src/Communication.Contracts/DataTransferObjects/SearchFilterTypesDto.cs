namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Defines search filter type
/// </summary>
public enum SearchFilterTypesDto
{
    /// <summary>
    ///     Text search using full-text search of database
    /// </summary>
    TextSearch = 0,

    /// <summary>
    ///     Filter of attributes, where attribute name are defined.
    /// </summary>
    AttributeFilter = 1
}