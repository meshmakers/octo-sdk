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
    ///     Downloads the job result as binary file
    /// </summary>
    /// <param name="tenantId">Tenant ID of the job</param>
    /// <param name="id">ID of the job</param>
    /// <returns></returns>
    Task<byte[]> DownloadExportRtResultAsync(string tenantId, string id);
    
    /// <summary>
    ///     Reconfigure the log level of the service.
    /// </summary>
    /// <param name="loggerName">Logger pattern name, e. g. Microsoft.*</param>
    /// <param name="minLogLevel">Minimal log level to be logged.</param>
    /// <param name="maxLogLevel">Maximum log level to be logged.</param>
    /// <returns></returns>
    Task ReconfigureLogLevelAsync(string loggerName, LogLevelDto minLogLevel, LogLevelDto maxLogLevel);
}