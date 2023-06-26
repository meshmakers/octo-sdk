using System.Collections.Generic;

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
    public IEnumerable<TDtoType> Items { get; set; } = null!;
}
