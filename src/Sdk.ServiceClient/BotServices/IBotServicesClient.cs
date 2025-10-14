using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

namespace Meshmakers.Octo.Sdk.ServiceClient.BotServices;

/// <summary>
///     Implementation of the client proxy for bot services.
/// </summary>
public interface IBotServicesClient : IServiceClient
{
    /// <summary>
    ///     Gets the status of an import job
    /// </summary>
    /// <param name="id">ID of the job</param>
    /// <returns></returns>
    Task<JobDto> GetImportJobStatus(string id);

    /// <summary>
    ///     Downloads the job result as a binary file
    /// </summary>
    /// <param name="tenantId">Tenant ID of the job</param>
    /// <param name="id">ID of the job</param>
    /// <returns></returns>
    Task<byte[]> DownloadExportRtResultAsync(string tenantId, string id);

    /// <summary>
    /// Start a job to run a fixup script for the given tenant.
    /// </summary>
    /// <param name="tenantId">Tenant ID of the job</param>
    /// <returns>The job response containing the job ID.</returns>
    Task<JobResponseDto> StartRunFixupScriptAsync(string tenantId);

    /// <summary>
    /// Restores the repository for the given tenant.
    /// The file must be a gzipped tar file containing the repository data.
    /// </summary>
    /// <param name="tenantId">The tenant ID for which the repository should be restored.</param>
    /// <param name="databaseName">The name of the database to restore.</param>
    /// <param name="filePath">The file path to the gzipped tar file containing the repository data.</param>
    /// <param name="oldDatabaseName">The (optional) name of the old db. This is required when restoring under differnet name</param>
    /// <returns>The job response containing the job ID.</returns>
    Task<JobResponseDto> RestoreRepositoryAsync(string tenantId, string databaseName, string filePath, string? oldDatabaseName = null);

    /// <summary>
    /// Dumps the repository for the given tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID for which the repository should be restored.</param>
    /// <returns>The job response containing the job ID.</returns>
    Task<JobResponseDto> StartDumpRepositoryAsync(string tenantId);

    /// <summary>
    ///     Reconfigure the log level of the service.
    /// </summary>
    /// <param name="loggerName">Logger pattern name, e. g. Microsoft.*</param>
    /// <param name="minLogLevel">Minimal log level to be logged.</param>
    /// <param name="maxLogLevel">Maximum log level to be logged.</param>
    /// <returns></returns>
    Task ReconfigureLogLevelAsync(string loggerName, LogLevelDto minLogLevel, LogLevelDto maxLogLevel);

    /// <summary>
    /// Compares two live tenants.
    /// </summary>
    /// <param name="sourceTenantId">The source tenant ID.</param>
    /// <param name="targetTenantId">The target tenant ID.</param>
    /// <param name="optionsJson">Optional JSON string of comparison options.</param>
    /// <returns>The job response containing the job ID.</returns>
    Task<JobResponseDto> CompareLiveTenantsAsync(string sourceTenantId, string targetTenantId,
        string? optionsJson = null);

    /// <summary>
    /// Compares a live tenant with a backup archive.
    /// </summary>
    /// <param name="sourceTenantId">The live tenant ID.</param>
    /// <param name="backupFilePath">The file path to the backup file (.tar.gz).</param>
    /// <param name="optionsJson">Optional JSON string of comparison options.</param>
    /// <returns>The job response containing the job ID.</returns>
    Task<JobResponseDto> CompareLiveTenantWithBackupAsync(string sourceTenantId, string backupFilePath,
        string? optionsJson = null);

    /// <summary>
    /// Compares two backup archives.
    /// </summary>
    /// <param name="sourceBackupFilePath">The file path to the source backup file (.tar.gz).</param>
    /// <param name="targetBackupFilePath">The file path to the target backup file (.tar.gz).</param>
    /// <param name="optionsJson">Optional JSON string of comparison options.</param>
    /// <returns>The job response containing the job ID.</returns>
    Task<JobResponseDto> CompareBackupsAsync(string sourceBackupFilePath,
        string targetBackupFilePath, string? optionsJson = null);

    /// <summary>
    /// Compares a backup archive with a live tenant.
    /// </summary>
    /// <param name="backupFilePath">The file path to the backup file (.tar.gz).</param>
    /// <param name="targetTenantId">The target live tenant ID.</param>
    /// <param name="optionsJson">Optional JSON string of comparison options.</param>
    /// <returns>The job response containing the job ID.</returns>
    Task<JobResponseDto> CompareBackupWithLiveTenantAsync(string backupFilePath,
        string targetTenantId, string? optionsJson = null);
}