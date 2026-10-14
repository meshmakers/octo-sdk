using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts.Serialization;
using Meshmakers.Octo.ConstructionKit.Contracts.Services;
using Meshmakers.Octo.ConstructionKit.Engine.Resolvers.RangeRetention;
using Meshmakers.Octo.Sdk.SourceGeneration;
using Microsoft.Extensions.DependencyInjection;

namespace Sdk.SourceGeneration.Tests;

/// <summary>
///     AB#6715: octo-cli's GraphQlDtos failed to build once a range-retaining model (System.Bot built after the
///     range-retention flip, AB#6675) was restored: the compiled yaml references its dependencies major-qualified
///     (<c>System@2/Name-1</c>), the restored cache holds concrete ids, and the DTO generator threw
///     <c>CkCacheException: CkAttributeId 'System@2/Name-1' not found in CkCache</c>. The fixtures are real
///     <c>octo-ckc Compile</c> output (range retention on) of a one-type model on System-2.5.0.
/// </summary>
public class RangeRetentionDtoGenerationTests
{
    private const string TenantId = "dto-generator";

    private static readonly string FixtureDirectory = Path.Combine(AppContext.BaseDirectory, "Fixtures");

    private static (CkCompiledModelRoot Model, ICkCacheService Cache) Restore()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddConstructionKit();
        var provider = services.BuildServiceProvider();

        var cache = provider.GetRequiredService<ICkCacheService>();
        cache.CreateTenant(TenantId);
        cache.RestoreCache(TenantId, File.ReadAllText(Path.Combine(FixtureDirectory, "ck-dtofixture.cache.json")));

        var yamlFile = Path.Combine(FixtureDirectory, "ck-dtofixture.yaml");
        var model = provider.GetRequiredService<ICkYamlSerializer>()
            .DeserializeCompiledModelRoot(File.ReadAllText(yamlFile), yamlFile, new OperationResult());
        return (model, cache);
    }

    [Fact]
    public void RangeRetainingModel_ReferencesAreMajorQualified_AndFailWithoutTheBinding()
    {
        var (model, cache) = Restore();

        Assert.Equal("System@2/Name-1", model.Types!.Single().Attributes!.Single().CkAttributeId.FullName);
        // The failure of AB#6715 (what octo-cli's GraphQlDtos build hit).
        Assert.Throws<CkCacheException>(() =>
            QueryDtoCodeGenerator.Instance.GenerateType("Ns", model.Types!.Single(), TenantId, cache));
    }

    [Fact]
    public void RangeRetainingModel_GeneratesQueryAndMutationDtos_AfterBindingToTheCache()
    {
        var (model, cache) = Restore();

        var unbound = CkDtoModelBinder.BindToCache(model, cache, TenantId);

        Assert.Empty(unbound);
        var type = model.Types!.Single();
        Assert.Equal("System-2.5.0/Name-1", type.Attributes!.Single().CkAttributeId.FullName);
        var query = QueryDtoCodeGenerator.Instance.GenerateType("Ns", type, TenantId, cache);
        var mutation = MutationDtoCodeGenerator.Instance.GenerateType("Ns", type, TenantId, cache);
        Assert.Contains("Name", query);
        Assert.Contains("Name", mutation);
    }

    [Fact]
    public void UnboundReference_IsReturned_InsteadOfAnOpaqueCacheException()
    {
        var (model, cache) = Restore();
        // A major the restored cache does not hold (cache: System-2.5.0).
        CkReferenceRewriter.Rewrite(model, id => id.IsMajorQualified ? CkModelId.MajorQualified(id.Name, 3) : null);

        var unbound = CkDtoModelBinder.BindToCache(model, cache, TenantId);

        Assert.Contains("type System@3/Entity-1", unbound);
    }

    [Fact]
    public void ClassicModel_NeedsNoBinding()
    {
        var (model, cache) = Restore();
        CkReferenceRewriter.Rewrite(model, id => id.IsMajorQualified ? new CkModelId(id.Name, "2.5.0") : null);

        Assert.Empty(CkDtoModelBinder.BindToCache(model, cache, TenantId));
        var code = QueryDtoCodeGenerator.Instance.GenerateType("Ns", model.Types!.Single(), TenantId, cache);
        Assert.Contains("Name", code);
    }
}
