using System.Collections.Generic;
using Meshmakers.Octo.Common.Shared.DataTransferObjects;

namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;

/// <summary>
/// Data transfer object for a list of items of a connection.
/// </summary>
/// <typeparam name="TDtoType"></typeparam>
public class QlItemsContainer<TDtoType>
{
    /// <summary>
    /// Returns the deserialized items list of the given type.
    /// </summary>
    public IEnumerable<TDtoType>? Items { get; set; }
    
    /// <summary>
    /// Returns the deserialized grouping list if requested.
    /// </summary>
    public IEnumerable<GroupingDto>? Grouping { get; set; }
    
    /// <summary>
    /// Returns the total count
    /// </summary>
    public long? TotalCount { get; set; }
}
