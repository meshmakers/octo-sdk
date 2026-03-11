using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;

namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.System;

/// <summary>
///     Interface of the client proxy for asset services on system level.
/// </summary>
public interface IAssetServicesClient : IServiceClient
{
    /// <summary>
    ///     Gets the status of an import job.
    /// </summary>
    /// <param name="id">The identifier of the import job.</param>
    /// <returns></returns>
    Task<JobDto> GetImportJobStatusAsync(string id);

    /// <summary>
    ///     Imports a construction kit model.
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="ckModelFilePath">File path to the construction kit model file
    /// that can be a JSON or a zipped JSON file.</param>
    /// <returns></returns>
    Task<string> ImportCkModelAsync(string tenantId, string ckModelFilePath);

    /// <summary>
    ///     Imports a runtime model
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="importStrategy">Import strategy for the runtime model.</param>
    /// <param name="rtModelFilePath">File path to the runtime file that can be a JSON or a zipped JSON file.</param>
    /// <returns></returns>
    Task<string> ImportRtModelAsync(string tenantId, ImportStrategyDto importStrategy, string rtModelFilePath);

    /// <summary>
    ///     Exports a runtime model by a query.
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="queryId">The query object identifier whose result is exported.</param>
    /// <returns></returns>
    Task<string> ExportRtModelByQueryAsync(string tenantId, OctoObjectId queryId);

    /// <summary>
    ///     Exports a runtime model by deep graph.
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="originRtIds">Origin runtime identifiers</param>
    /// <param name="originCkTypeId">Origin construction kit type identifier</param>
    /// <returns></returns>
    Task<string> ExportRtModelByDeepGraphAsync(string tenantId, IEnumerable<OctoObjectId> originRtIds,
        RtCkId<CkTypeId> originCkTypeId);

    /// <summary>
    ///     Resets a child tenant to its initial state.
    /// </summary>
    /// <remarks>
    ///     Resets a child tenant to its initial state. This means that all data of the tenant is deleted and the
    ///     construction kit is reset to the system-only models.
    /// </remarks>
    /// <param name="childTenantId">Child tenant identifier</param>
    /// <returns></returns>
    Task CleanTenantAsync(string childTenantId);

    /// <summary>
    ///     Updates the system construction kit model of a child tenant.
    /// </summary>
    /// <param name="childTenantId">Child tenant identifier</param>
    /// <returns></returns>
    Task UpdateSystemCkModelOfTenant(string childTenantId);

    /// <summary>
    ///     Clears the cache of a child tenant.
    /// </summary>
    /// <remarks>
    ///     By executing this action, the cache of a child tenant is cleared.
    ///     This means that all cached data of the tenant is deleted.
    ///     That may result in a performance decrease of the tenant and unavailability of
    ///     services for a certain time.
    /// </remarks>
    /// <param name="childTenantId">Child tenant identifier</param>
    /// <returns></returns>
    Task ClearTenantCacheAsync(string childTenantId);

    /// <summary>
    ///     Returns a list of all child tenants.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<TenantDto>> GetTenantsAsync();

    /// <summary>
    ///     Creates a new child tenant.
    /// </summary>
    /// <param name="childTenantId">Child tenant identifier</param>
    /// <param name="databaseName">Name of the database</param>
    /// <returns></returns>
    Task CreateTenantAsync(string childTenantId, string databaseName);

    /// <summary>
    ///     Attaches a child tenant.
    /// </summary>
    /// <remarks>
    ///     The Database must exist and the tenant will be added as a child tenant.
    /// </remarks>
    /// <param name="childTenantId">Child tenant identifier</param>
    /// <param name="databaseName">Name of the database</param>
    /// <returns></returns>
    Task AttachTenantAsync(string childTenantId, string databaseName);

    /// <summary>
    ///     Detaches a child tenant.
    /// </summary>
    /// <remarks>
    ///     The Database won't be deleted, but the child tenant will be removed.
    /// </remarks>
    /// <param name="childTenantId">Child tenant identifier</param>
    /// <returns></returns>
    Task DetachTenantAsync(string childTenantId);

    /// <summary>
    ///     Deletes a child tenant.
    /// </summary>
    /// <param name="childTenantId">Child tenant identifier</param>
    /// <returns></returns>
    Task DeleteTenantAsync(string childTenantId);

    /// <summary>
    ///     Reconfigure the log level of the service.
    /// </summary>
    /// <param name="loggerName">Logger pattern name, e. g. Microsoft.*</param>
    /// <param name="minLogLevel">Minimal log level to be logged.</param>
    /// <param name="maxLogLevel">Maximum log level to be logged.</param>
    /// <returns></returns>
    Task ReconfigureLogLevelAsync(string loggerName, LogLevelDto minLogLevel, LogLevelDto maxLogLevel);
}