// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Blueprints;

public class BlueprintCatalogItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CatalogName { get; set; } = string.Empty;
}

public class BlueprintCatalogListResponseDto
{
    public List<BlueprintCatalogItemDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
}

public class BlueprintApplyRequestDto
{
    public string BlueprintId { get; set; } = string.Empty;
    public bool Force { get; set; }
}

public class BlueprintApplyResultDto
{
    public bool Success { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string BlueprintId { get; set; } = string.Empty;
    public string ApplicationMode { get; set; } = string.Empty;
    public int SeedDataFilesApplied { get; set; }
    public List<string> LoadedCkModels { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

public class BlueprintHistoryItemDto
{
    public string BlueprintId { get; set; } = string.Empty;
    public DateTime AppliedAt { get; set; }
    public string ApplicationMode { get; set; } = string.Empty;
    public string? PreviousVersion { get; set; }
    public int EntitiesCreated { get; set; }
    public int EntitiesUpdated { get; set; }
    public int EntitiesDeleted { get; set; }
    public string? SeedDataChecksum { get; set; }
}

public class BlueprintUpdateInfoDto
{
    public string? CurrentBlueprintId { get; set; }
    public string? CurrentVersion { get; set; }
    public string? RecommendedVersion { get; set; }
    public bool HasUpdate { get; set; }
    public List<string> AvailableVersions { get; set; } = [];
}

public class BlueprintUpdateRequestDto
{
    public string TargetVersion { get; set; } = string.Empty;
    public string UpdateMode { get; set; } = "Merge";
    public bool CreateBackup { get; set; } = true;
    public bool DryRun { get; set; }
    public Dictionary<string, string>? ConflictResolutions { get; set; }
}

public class BlueprintUpdatePreviewDto
{
    public string TargetVersion { get; set; } = string.Empty;
    public int EntitiesToAdd { get; set; }
    public int EntitiesToUpdate { get; set; }
    public int EntitiesToDelete { get; set; }
    public List<BlueprintConflictDto> Conflicts { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

public class BlueprintConflictDto
{
    public string EntityId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? SuggestedResolution { get; set; }
}

public class BlueprintBackupDto
{
    public string BackupId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string BlueprintId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public long? SizeBytes { get; set; }
}

public class BlueprintRestoreResultDto
{
    public bool Success { get; set; }
    public int EntitiesRestored { get; set; }
    public List<string> Messages { get; set; } = [];
}

public class BlueprintInstallationDto
{
    public string BlueprintId { get; set; } = string.Empty;
    public DateTime InstalledAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public bool IsDependency { get; set; }
    public List<string> ResolvedDependencies { get; set; } = [];
    public string? SeedDataChecksum { get; set; }
}

public class BlueprintUninstallResultDto
{
    public bool Success { get; set; }
    public string? UninstalledBlueprintId { get; set; }
    public int EntitiesDeleted { get; set; }
    public List<string> CascadedDependencies { get; set; } = [];
    public List<string> BlockingDependents { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}
