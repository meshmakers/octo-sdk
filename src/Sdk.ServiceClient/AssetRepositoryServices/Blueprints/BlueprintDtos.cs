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
    public bool DryRun { get; set; }
    public Dictionary<string, string>? ConflictResolutions { get; set; }

    /// <summary>
    ///     Confirms that the update may blank EVERY attribute listed in the preview's
    ///     <see cref="BlueprintUpdatePreviewDto.BlankedAttributes" /> (AB#6316, server contract AB#6315).
    ///     Default <c>false</c>: tenant values are kept. Prefer <see cref="ConfirmedBlankings" />.
    /// </summary>
    public bool AllowBlanking { get; set; }

    /// <summary>
    ///     Confirms blanking for exactly these entity/attribute pairs; everything else stays kept.
    ///     Ignored by the server when <see cref="AllowBlanking" /> is true. <c>null</c> or empty means
    ///     no confirmation.
    /// </summary>
    public List<BlueprintBlankingConfirmationDto>? ConfirmedBlankings { get; set; }
}

/// <summary>
///     Explicit confirmation that one attribute of one entity may be blanked by a blueprint update
///     (AB#6316). Take the values from <see cref="BlueprintBlankedAttributeDto" />.
/// </summary>
public class BlueprintBlankingConfirmationDto
{
    public string RtId { get; set; } = string.Empty;
    public string AttributeName { get; set; } = string.Empty;
}

/// <summary>
///     One attribute whose non-empty tenant value a blueprint update would blank (AB#6316). The
///     server describes values by kind and size only (they may be credentials), so neither this
///     type nor anything printed from it contains a value.
/// </summary>
public class BlueprintBlankedAttributeDto
{
    public string RtId { get; set; } = string.Empty;
    public string CkTypeId { get; set; } = string.Empty;
    public string AttributeName { get; set; } = string.Empty;

    /// <summary><c>SeedEmpty</c> or <c>SeedOmitted</c>.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>What the tenant holds, e.g. <c>string (223 chars)</c>.</summary>
    public string? CurrentSummary { get; set; }

    /// <summary>What the seed carries, e.g. <c>empty string</c> or <c>omitted</c>.</summary>
    public string? IncomingSummary { get; set; }

    /// <summary>
    ///     <c>false</c>: the tenant value is kept. <c>true</c>: blanked on confirmation (apply result
    ///     only; always <c>false</c> in a preview).
    /// </summary>
    public bool AppliedOnUpdate { get; set; }
}

/// <summary>
///     Result of a blueprint update apply (REST 200 since AB#6315). Services older than that answer
///     204 without a body; the client then returns <see cref="Success" /> = true with empty lists.
/// </summary>
public class BlueprintUpdateResultDto
{
    public bool Success { get; set; }
    public int EntitiesAdded { get; set; }
    public int EntitiesUpdated { get; set; }
    public int EntitiesUnchanged { get; set; }
    public int EntitiesDeleted { get; set; }
    public int EntitiesSkipped { get; set; }
    public List<string> Warnings { get; set; } = [];

    /// <summary>Attributes the seed would have blanked, each with <c>AppliedOnUpdate</c>.</summary>
    public List<BlueprintBlankedAttributeDto> BlankedAttributes { get; set; } = [];
}

public class BlueprintUpdatePreviewDto
{
    public string TargetVersion { get; set; } = string.Empty;
    public int EntitiesToAdd { get; set; }
    public int EntitiesToUpdate { get; set; }
    /// <summary>
    ///     Blueprint-managed entities the update re-applies without changing an attribute (AB#5297).
    ///     Absent on services older than 3.4.126 - reads as 0.
    /// </summary>
    public int EntitiesUnchanged { get; set; }
    public int EntitiesToDelete { get; set; }
    public List<BlueprintConflictDto> Conflicts { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    /// <summary>
    ///     Attribute-level diff per entity counted in <see cref="EntitiesToUpdate" /> (AB#5297, AB#5308).
    ///     Values are text: scalars verbatim, records and lists as JSON. Absent on services older than
    ///     3.4.126 - reads as an empty list.
    /// </summary>
    public List<BlueprintEntityChangeDto> Changes { get; set; } = [];

    /// <summary>
    ///     Attributes whose non-empty tenant value the seed would blank (AB#6316, server AB#6315).
    ///     Summaries only, never values. Absent on older services - reads as an empty list.
    /// </summary>
    public List<BlueprintBlankedAttributeDto> BlankedAttributes { get; set; } = [];
}

public class BlueprintEntityChangeDto
{
    public string EntityId { get; set; } = string.Empty;
    public string? EntityWellKnownName { get; set; }
    public string? EntityDisplayName { get; set; }
    public string EntityCkTypeId { get; set; } = string.Empty;
    public List<BlueprintAttributeChangeDto> Attributes { get; set; } = [];
    public string? Note { get; set; }
}

public class BlueprintAttributeChangeDto
{
    public string AttributeName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}

public class BlueprintConflictDto
{
    public string EntityId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? SuggestedResolution { get; set; }
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

public class BlueprintCatalogRefreshResultDto
{
    public string CatalogName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
}

public class BlueprintCatalogRefreshResponseDto
{
    public List<BlueprintCatalogRefreshResultDto> Results { get; set; } = [];
}
