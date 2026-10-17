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

    /// <summary>
    /// AB#6868: blockers, <c>isBlocked</c> and <c>isRangeRetaining</c> are optional members - a response of an asset
    /// repository that predates them deserializes with the neutral defaults.
    /// </summary>
    [Fact]
    public void Deserialize_ReadsBlockers_AndAnOldResponseWithoutThemIsNotBlocked()
    {
        var blocked = JsonSerializer.Deserialize<CkModelLibraryStatusItemDto>(
            """{"name":"Basic","hasUpdate":true,"needsAction":false,"isBlocked":true,"isRangeRetaining":false,"blockers":[{"dependent":"EnergyCommunity.Registration-2.8.0","update":"Basic-2.5.0","kind":"ExactPin","subject":"Basic-2.4.0","code":"CK_UPGRADE_EXACT_PIN","message":"exact pin Basic-2.4.0","dependentIsInUpdateSet":false,"suggestedDependentUpdate":"EnergyCommunity.Registration-2.10.0","resolveHint":"update EnergyCommunity.Registration to 2.10.0 first"}]}""",
            Web)!;
        var old = JsonSerializer.Deserialize<CkModelLibraryStatusItemDto>(
            """{"name":"Basic","hasUpdate":true,"needsAction":true}""", Web)!;

        Assert.True(blocked.IsBlocked);
        var blocker = Assert.Single(blocked.Blockers);
        Assert.Equal("EnergyCommunity.Registration-2.8.0", blocker.Dependent);
        Assert.Equal("ExactPin", blocker.Kind);
        Assert.Equal("CK_UPGRADE_EXACT_PIN", blocker.Code);
        Assert.Equal("update EnergyCommunity.Registration to 2.10.0 first", blocker.ResolveHint);
        Assert.False(old.IsBlocked);
        Assert.Empty(old.Blockers);
        Assert.False(old.IsRangeRetaining);
        Assert.True(old.NeedsAction);
    }

    [Fact]
    public void Deserialize_UpgradeCheckResponse_ReadsBlockers_AndAnOldResponseIsNotBlocked()
    {
        var blocked = JsonSerializer.Deserialize<UpgradeCheckResponseDto>(
            """{"modelName":"Basic","targetVersion":"2.5.0","hasBreakingChanges":false,"isBlocked":true,"blockers":[{"dependent":"A-1.0.0","update":"Basic-2.5.0","kind":"ExactPin","subject":"Basic-2.4.0","code":"CK_UPGRADE_EXACT_PIN","message":"m"}]}""",
            Web)!;
        var old = JsonSerializer.Deserialize<UpgradeCheckResponseDto>(
            """{"modelName":"Basic","targetVersion":"2.5.0","hasBreakingChanges":false}""", Web)!;

        Assert.True(blocked.IsBlocked);
        Assert.Single(blocked.Blockers);
        Assert.False(old.IsBlocked);
        Assert.Empty(old.Blockers);
    }
}
