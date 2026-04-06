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
    public List<string> JobIds { get; set; } = [];
}

public class CkModelLibraryStatusItemDto
{
    public string Name { get; set; } = string.Empty;
    public string? InstalledVersion { get; set; }
    public string? ModelState { get; set; }
    public List<string> Dependencies { get; set; } = [];
    public string? CatalogVersion { get; set; }
    public bool HasUpdate { get; set; }
    public bool NeedsAction { get; set; }
    public string? CatalogName { get; set; }
    public string? FullModelId { get; set; }
    public bool IsServiceManaged { get; set; }
    public bool IsCompatible { get; set; } = true;
    public string? IncompatibilityReason { get; set; }
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
}
