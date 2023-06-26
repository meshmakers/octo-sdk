using System.Collections.Generic;

namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;

public class QlItemsContainer<TDtoType>
{
    public IEnumerable<TDtoType> Items { get; set; } = null!;
}
