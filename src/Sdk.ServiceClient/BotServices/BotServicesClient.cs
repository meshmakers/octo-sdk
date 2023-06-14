using System;
using System.Threading.Tasks;
using Meshmakers.Common.Shared;
using Meshmakers.Octo.Common.Shared;
using Meshmakers.Octo.Common.Shared.DataTransferObjects;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Meshmakers.Octo.Sdk.Client.BotServices;

public class BotServicesClient : ServiceClient, IBotServicesClient
{
    public BotServicesClient(IOptions<BotServiceClientOptions> botServiceClientOptions,
        IBotServiceClientAccessToken botAccessToken)
        : this(botServiceClientOptions.Value, botAccessToken)
    {
    }

    public BotServicesClient(BotServiceClientOptions botServiceClientOptions,
        IBotServiceClientAccessToken botAccessToken)
        : base(botServiceClientOptions, botAccessToken)
    {
    }

    public async Task<JobDto> GetImportJobStatus(string id)
    {
        ArgumentValidation.ValidateString(nameof(id), id);

        var request = new RestRequest("jobs");
        request.AddQueryParameter("id", id);

        var response = await Client.ExecuteAsync<JobDto>(request);
        ValidateResponse(response);

        return response.Data!;
    }

    /// <summary>
    ///     Downloads the job result as binary file
    /// </summary>
    /// <param name="id">Job id</param>
    /// <returns></returns>
    public async Task<byte[]> DownloadExportRtResultAsync(string id)
    {
        ArgumentValidation.ValidateString(nameof(id), id);

        var request = new RestRequest("jobs/download");
        request.AddQueryParameter("id", id);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);

        return response.RawBytes!;
    }

    protected override Uri BuildServiceUri()
    {
        if (string.IsNullOrWhiteSpace(Options.EndpointUri))
        {
            throw new ServiceConfigurationMissingException("Job services URI is missing.");
        }

        return new Uri(Options.EndpointUri).Append("system").Append("v1");
    }
}
