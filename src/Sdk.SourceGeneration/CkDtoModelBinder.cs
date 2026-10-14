using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts.Services;
using Meshmakers.Octo.ConstructionKit.Engine.Resolvers.RangeRetention;

namespace Meshmakers.Octo.Sdk.SourceGeneration;

/// <summary>
///     A range-retaining compiled model (CK v2, AB#6675) references its dependencies major-qualified
///     (<c>System@2/Enabled-1</c>) while the compile cache the generator restores holds the concrete versions the
///     model was resolved against (<c>System-2.5.0/Enabled-1</c>). Binds the references to the cache before any
///     per-element generator looks them up - the same step the model generator performs (D1) - otherwise the lookup
///     throws <c>CkCacheException: CkAttributeId 'System@2/Enabled-1' not found in CkCache</c> (AB#6715).
/// </summary>
internal static class CkDtoModelBinder
{
    /// <summary>
    ///     Binds the major-qualified references of <paramref name="compiledModel" /> (in place) to the models of the
    ///     restored cache of <paramref name="tenantId" />.
    /// </summary>
    /// <returns>The references that stay unbound (<c>kind Model@N/Element</c>), empty when everything is bound.</returns>
    public static IReadOnlyList<string> BindToCache(CkCompiledModelRoot compiledModel, ICkCacheService cacheService,
        string tenantId)
    {
        if (!CkReferenceRewriter.HasMajorQualifiedReferences(compiledModel))
        {
            return [];
        }

        CkReferenceRewriter.BindMajorQualified(compiledModel, cacheService.GetCkModelIds(tenantId));
        return CkReferenceRewriter.CollectReferences(compiledModel)
            .Where(r => r.ModelId.IsMajorQualified)
            .Select(r => $"{r.Kind} {r.ModelId.FullName}/{r.ElementId}")
            .Distinct()
            .OrderBy(r => r, StringComparer.Ordinal)
            .ToList();
    }
}
