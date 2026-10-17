// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.CkModelCatalog;

public class CkModelCatalogDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class CkModelCatalogItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CatalogName { get; set; } = string.Empty;
}

public class CkModelCatalogListResponseDto
{
    public List<CkModelCatalogItemDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
}

public class ImportFromCatalogRequestDto
{
    public string CatalogName { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
}

public class ImportFromCatalogBatchRequestDto
{
    public string CatalogName { get; set; } = string.Empty;
    public List<string> ModelIds { get; set; } = [];
}

public class BatchImportResponseDto
{
    public string JobId { get; set; } = string.Empty;
}

public class CkModelLibraryStatusItemDto
{
    public string Name { get; set; } = string.Empty;
    public string? InstalledVersion { get; set; }
    public string? ModelState { get; set; }

    /// <summary>
    ///     AB#6674: why the installed model is <c>ResolveFailed</c> - plain English text naming models, versions, ranges
    ///     and pins; <c>null</c> otherwise (also against an asset repository that predates the field). Display only,
    ///     never parse it.
    /// </summary>
    public string? ResolveFailedReason { get; set; }

    public List<string> Dependencies { get; set; } = [];
    public string? CatalogVersion { get; set; }
    public bool HasUpdate { get; set; }
    public bool NeedsAction { get; set; }
    public string? CatalogName { get; set; }
    public string? FullModelId { get; set; }
    public bool IsServiceManaged { get; set; }
    public bool IsCompatible { get; set; } = true;
    public string? IncompatibilityReason { get; set; }

    /// <summary>
    ///     AB#6868 (tenant upgrade guard, read-only): true when importing the offered catalog version
    ///     (<see cref="CatalogVersion" />) would break an installed model. A blocked row keeps <see cref="HasUpdate" />
    ///     <c>true</c> and has <see cref="NeedsAction" /> <c>false</c>. <c>false</c> against an asset repository that
    ///     predates the member.
    /// </summary>
    public bool IsBlocked { get; set; }

    /// <summary>
    ///     AB#6868: why the row is blocked (empty when <see cref="IsBlocked" /> is <c>false</c>), one entry per
    ///     installed model and pin the update would break.
    /// </summary>
    public List<CkUpgradeBlockerDto> Blockers { get; set; } = [];

    /// <summary>
    ///     AB#6868: true when the installed model declares dependency ranges (range-retaining, CK language 2); false for
    ///     an exact-pinned classic model, a catalog-only row, or an asset repository that predates the member.
    /// </summary>
    public bool IsRangeRetaining { get; set; }
}

/// <summary>
///     AB#6868: one reason why an installed model would break if an offered update were imported (tenant upgrade
///     guard, AB#6867). Language-neutral data (<see cref="Code" />, <see cref="Dependent" />, <see cref="Update" />,
///     <see cref="Subject" />) plus an English <see cref="Message" /> for clients that do not localize.
/// </summary>
public class CkUpgradeBlockerDto
{
    /// <summary>The model that would break, full id (for example <c>EnergyCommunity.Registration-2.8.0</c>).</summary>
    public string Dependent { get; set; } = string.Empty;

    /// <summary>The model of the update that causes it, full id (for example <c>Basic-2.5.0</c>).</summary>
    public string Update { get; set; } = string.Empty;

    /// <summary>
    ///     Blocker kind: <c>ExactPin</c>, <c>FloorNotMet</c>, <c>RangeNotSatisfied</c>, <c>ElementRemoved</c>,
    ///     <c>ElementChanged</c>, <c>ElementBecameInternal</c> or <c>NameCollision</c>. New kinds may appear.
    /// </summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>The pin (<c>Basic-2.4.0</c>), range or element / member the blocker is about.</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>Stable code per kind, for example <c>CK_UPGRADE_EXACT_PIN</c>.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>English explanation, for example <c>exact pin Basic-2.4.0: Basic-2.5.0 replaces it</c>.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>True when <see cref="Dependent" /> is itself a model of the evaluated update set.</summary>
    public bool DependentIsInUpdateSet { get; set; }

    /// <summary>
    ///     A newer catalog version of the dependent (for example <c>EnergyCommunity.Registration-2.10.0</c>) that was
    ///     proven to resolve this blocker together with the update; <c>null</c> when none is offered or none was proven.
    /// </summary>
    public string? SuggestedDependentUpdate { get; set; }

    /// <summary>
    ///     Human-readable next step when <see cref="SuggestedDependentUpdate" /> is set, for example
    ///     <c>update EnergyCommunity.Registration to 2.10.0 first</c>; <c>null</c> otherwise.
    /// </summary>
    public string? ResolveHint { get; set; }
}

public class CkModelLibraryStatusResponseDto
{
    public List<CkModelLibraryStatusItemDto> Items { get; set; } = [];
    public int ModelsNeedingActionCount { get; set; }
}

public class DependencyResolutionItemDto
{
    public string ModelId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RequiredVersion { get; set; } = string.Empty;
    public string? InstalledVersion { get; set; }
    public string Action { get; set; } = string.Empty;
    public List<DependencyResolutionItemDto> Dependencies { get; set; } = [];
}

public class DependencyResolutionResponseDto
{
    public DependencyResolutionItemDto RootModel { get; set; } = null!;
}

public class BatchDependencyResolutionResponseDto
{
    public List<string> ModelsToImport { get; set; } = [];
    public List<DependencyResolutionResponseDto> DependencyTrees { get; set; } = [];
}

public class UpgradeCheckResponseDto
{
    public string ModelName { get; set; } = string.Empty;
    public string? InstalledVersion { get; set; }
    public string TargetVersion { get; set; } = string.Empty;
    public bool UpgradeNeeded { get; set; }
    public bool MigrationPathAvailable { get; set; }
    public bool HasBreakingChanges { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>
    ///     AB#6868: true when importing the target version would break an installed model (tenant upgrade guard,
    ///     read-only). Independent of <see cref="HasBreakingChanges" />, which judges the model's own migration.
    ///     <c>false</c> against an asset repository that predates the member.
    /// </summary>
    public bool IsBlocked { get; set; }

    /// <summary>AB#6868: the installed models the target version would break (empty when not blocked).</summary>
    public List<CkUpgradeBlockerDto> Blockers { get; set; } = [];
}
