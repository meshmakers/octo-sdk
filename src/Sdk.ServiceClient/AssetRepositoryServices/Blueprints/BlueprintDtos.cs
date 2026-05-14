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
