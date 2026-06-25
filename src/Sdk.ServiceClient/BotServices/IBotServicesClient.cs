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
    ///     Downloads the job result as a binary file.
    /// </summary>
    /// <param name="tenantId">Tenant ID of the job</param>
    /// <param name="id">ID of the job</param>
    /// <returns></returns>
    [Obsolete("Use DownloadDumpToFileAsync for streaming downloads instead.")]
    Task<byte[]> DownloadExportRtResultAsync(string tenantId, string id);

    /// <summary>
    /// Downloads the dump result directly to a file using streaming (no full byte[] in RAM).
    /// </summary>
    /// <param name="tenantId">Tenant ID of the job.</param>
    /// <param name="jobId">ID of the completed dump job.</param>
    /// <param name="outputFilePath">The local file path to write the dump to.</param>
    /// <param name="progressCallback">Optional callback for download progress (bytes downloaded).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task DownloadDumpToFileAsync(string tenantId, string jobId, string outputFilePath,
        Action<long>? progressCallback = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Start a job to run a fixup script for the given tenant.
    /// </summary>
    /// <param name="tenantId">Tenant ID of the job</param>
    /// <returns>The job response containing the job ID.</returns>
    Task<JobResponseDto> StartRunFixupScriptAsync(string tenantId);

    /// <summary>
    /// Restores the repository using tus resumable upload protocol.
    /// The file is uploaded in chunks with resume support, then a restore job is started.
    /// </summary>
    /// <param name="tenantId">The tenant ID for which the repository should be restored.</param>
    /// <param name="databaseName">The name of the database to restore.</param>
    /// <param name="filePath">The file path to the gzipped tar file containing the repository data.</param>
    /// <param name="oldDatabaseName">The (optional) name of the old db. This is required when restoring under a different name.</param>
    /// <param name="restoreArchiveData">When <c>true</c>, CrateDB archive data contained in the backup artifact is restored alongside the Mongo data (AB#4231). Default <c>false</c> restores Mongo only (today's behaviour).</param>
    /// <param name="progressCallback">Optional callback reporting upload progress (0.0 to 1.0).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The job response containing the job ID.</returns>
    Task<JobResponseDto> RestoreRepositoryWithTusAsync(string tenantId, string databaseName, string filePath,
        string? oldDatabaseName = null,
        bool restoreArchiveData = false,
        Action<double>? progressCallback = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dumps the repository for the given tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID for which the repository should be restored.</param>
    /// <param name="includeArchiveData">When <c>true</c>, CrateDB archive data is included in the dump artifact (AB#4231). Default <c>false</c> dumps Mongo only (today's behaviour).</param>
    /// <returns>The job response containing the job ID.</returns>
    Task<JobResponseDto> StartDumpRepositoryAsync(string tenantId, bool includeArchiveData = false);

    /// <summary>
    ///     Reconfigure the log level of the service.
    /// </summary>
    /// <param name="loggerName">Logger pattern name, e. g. Microsoft.*</param>
    /// <param name="minLogLevel">Minimal log level to be logged.</param>
    /// <param name="maxLogLevel">Maximum log level to be logged.</param>
    /// <returns></returns>
    Task ReconfigureLogLevelAsync(string loggerName, LogLevelDto minLogLevel, LogLevelDto maxLogLevel);

    // ---- Archive data export/import (AB#4230) ----

    /// <summary>
    ///     Starts an async Hangfire job in bot-services that exports the row data of an archive to a
    ///     downloadable ZIP (delegating the CrateDB read to asset-repo over the SDK). When both
    ///     <paramref name="fromUtc"/> and <paramref name="toUtc"/> are <c>null</c> the whole archive is
    ///     exported; when supplied, the slice <c>[fromUtc, toUtc)</c> is exported. Poll the job and
    ///     download the result via the existing job plumbing. Archive data export/import concept §5.
    /// </summary>
    /// <param name="tenantId">The tenant that owns the archive.</param>
    /// <param name="archiveRtId">Runtime id of the <c>CkArchive</c> entity.</param>
    /// <param name="fromUtc">Inclusive lower bound of the exported window (UTC); null for whole archive.</param>
    /// <param name="toUtc">Exclusive upper bound of the exported window (UTC); null for whole archive.</param>
    /// <returns>The job response containing the job id.</returns>
    Task<JobResponseDto> StartExportArchiveDataAsync(string tenantId, string archiveRtId, DateTime? fromUtc,
        DateTime? toUtc);

    /// <summary>
    ///     Uploads an export ZIP via the tus resumable-upload protocol (same as repository restore)
    ///     and then starts an async import job in bot-services that validates the schema match and
    ///     streams the rows into the target archive. Archive data export/import concept §5.
    /// </summary>
    /// <param name="tenantId">The tenant that owns the archive.</param>
    /// <param name="archiveRtId">Runtime id of the target <c>CkArchive</c> entity.</param>
    /// <param name="filePath">Local path to the export ZIP to import.</param>
    /// <param name="mode">Insert-only or upsert; serialized as a string query parameter.</param>
    /// <param name="progressCallback">Optional callback reporting upload progress (0.0 to 1.0).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The job response containing the job id.</returns>
    Task<JobResponseDto> StartImportArchiveDataWithTusAsync(string tenantId, string archiveRtId, string filePath,
        ArchiveImportMode mode,
        Action<double>? progressCallback = null,
        CancellationToken cancellationToken = default);
}