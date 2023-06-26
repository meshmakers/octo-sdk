using System.Threading.Tasks;
using Meshmakers.Octo.Common.Shared.DataTransferObjects;

namespace Meshmakers.Octo.Sdk.ServiceClient.BotServices;

/// <summary>
/// Implementation of the client proxy for bot services.
/// </summary>
public interface IBotServicesClient : IServiceClient
{
    /// <summary>
    /// Gets the status of an import job
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<JobDto> GetImportJobStatus(string id);

    /// <summary>
    ///     Downloads the job result as binary file
    /// </summary>
    /// <param name="id">Job id</param>
    /// <returns></returns>
    Task<byte[]> DownloadExportRtResultAsync(string id);
}
