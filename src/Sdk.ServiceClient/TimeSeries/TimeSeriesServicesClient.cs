using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Meshmakers.Octo.Sdk.ServiceClient.TimeSeries;

/// <summary>
///     Implementation of the timeseries services client.
/// </summary>
public class TimeSeriesServicesClient : ServiceClient, ITimeSeriesServicesClient
{
    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="serviceClientOptions">Options for configuration of the client proxy.</param>
    /// <param name="timeSeriesAccessToken">The access token management object</param>
    public TimeSeriesServicesClient(IOptions<TimeSeriesServiceClientOptions> serviceClientOptions,
        ITimeSeriesServiceClientAccessToken timeSeriesAccessToken)
        : this(serviceClientOptions.Value, timeSeriesAccessToken)
    {
    }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="serviceClientOptions">Options for configuration of the client proxy.</param>
    /// <param name="timeSeriesAccessToken">The access token management object</param>
    public TimeSeriesServicesClient(TimeSeriesServiceClientOptions serviceClientOptions,
        ITimeSeriesServiceClientAccessToken timeSeriesAccessToken)
        : base(serviceClientOptions, timeSeriesAccessToken)
    {
    }

    /// <inheritdoc />
    public async Task EnableAsync(string tenantId)
    {
        var request = new RestRequest($"{tenantId}/enable", Method.Post);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task DisableAsync(string tenantId)
    {
        var request = new RestRequest($"{tenantId}/disable", Method.Post);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }


    /// <inheritdoc />
    protected override Uri BuildServiceUri()
    {
        if (string.IsNullOrWhiteSpace(Options.EndpointUri))
        {
            throw new ServiceConfigurationMissingException("Timeseries services URI is missing.");
        }

        return new Uri(Options.EndpointUri);
    }
}