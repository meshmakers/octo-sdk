using System.Threading.Tasks;
using Meshmakers.Octo.Common.Shared.DataTransferObjects;

namespace Meshmakers.Octo.Sdk.Client.BotServices;

public interface IBotServicesClient : IServiceClient
{
    Task<JobDto> GetImportJobStatus(string id);

    /// <summary>
    ///     Downloads the job result as binary file
    /// </summary>
    /// <param name="id">Job id</param>
    /// <returns></returns>
    Task<byte[]> DownloadExportRtResultAsync(string id);
}
