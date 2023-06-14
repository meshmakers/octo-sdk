using System.Collections.Generic;

namespace Meshmakers.Octo.Sdk.Client.AssetRepositoryServices.Tenants;

public class QlItemsContainer<TTdoType>
{
    public IEnumerable<TTdoType> Items { get; set; } = null!;
}
