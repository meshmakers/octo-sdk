using System.Collections.Generic;

namespace Meshmakers.Octo.Frontend.Client.Tenants;

public class QlItemsContainer<TTdoType>
{
    public IEnumerable<TTdoType> Items { get; set; }
}
