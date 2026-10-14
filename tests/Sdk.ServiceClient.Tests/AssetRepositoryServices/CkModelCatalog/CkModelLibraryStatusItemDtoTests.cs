using System.Text.Json;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.CkModelCatalog;

namespace Sdk.ServiceClient.Tests.AssetRepositoryServices.CkModelCatalog;

/// <summary>
/// AB#6674: the LibraryStatus item carries <c>resolveFailedReason</c> (camelCase on the wire); a response of an asset
/// repository that predates the field still deserializes, the reason is simply null.
/// </summary>
public class CkModelLibraryStatusItemDtoTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Deserialize_ReadsTheReason_AndAnOldResponseWithoutItIsNull()
    {
        var withReason = JsonSerializer.Deserialize<CkModelLibraryStatusItemDto>(
            """{"name":"Dep","modelState":"ResolveFailed","resolveFailedReason":"Unmet dependency: Base-[1.0,2.0) (floor 1.1.0): installed Base-1.0.0"}""",
            Web)!;
        var old = JsonSerializer.Deserialize<CkModelLibraryStatusItemDto>(
            """{"name":"Dep","modelState":"ResolveFailed"}""", Web)!;

        Assert.Equal("Unmet dependency: Base-[1.0,2.0) (floor 1.1.0): installed Base-1.0.0", withReason.ResolveFailedReason);
        Assert.Null(old.ResolveFailedReason);
    }
}
