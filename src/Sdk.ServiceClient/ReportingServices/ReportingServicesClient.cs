using Meshmakers.Common.Shared;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Meshmakers.Octo.Sdk.ServiceClient.ReportingServices;

/// <summary>
///     Implementation of the client proxy for bot services.
/// </summary>
public class ReportingServicesClient : ServiceClient, IReportingServicesClient
{
    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="serviceClientOptions">Options for configuration of the client proxy.</param>
    /// <param name="accessToken">The access token management object</param>
    public ReportingServicesClient(IOptions<ReportingServicesClientOptions> serviceClientOptions,
        IReportingServicesClientAccessToken accessToken)
        : this(serviceClientOptions.Value, accessToken)
    {
    }

    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="servicesClientOptions">Options for configuration of the client proxy.</param>
    /// <param name="accessToken">The access token management object</param>
    public ReportingServicesClient(ReportingServicesClientOptions servicesClientOptions,
        IReportingServicesClientAccessToken accessToken)
        : base(servicesClientOptions, accessToken)
    {
    }

    /// <inheritdoc />
    protected override Uri BuildServiceUri()
    {
        if (string.IsNullOrWhiteSpace(Options.EndpointUri))
        {
            throw new ServiceConfigurationMissingException("Reporting services URI is missing");
        }

        return new Uri(Options.EndpointUri).Append("system").Append("v1");
    }

    /// <inheritdoc />
    public async Task EnableAsync(string tenantId)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);

        var request = new RestRequest("reporting/enable", Method.Post);
        request.AddQueryParameter("tenantId", tenantId);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task DisableAsync(string tenantId)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);

        var request = new RestRequest("reporting/disable", Method.Post);
        request.AddQueryParameter("tenantId", tenantId);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }
        
    /// <inheritdoc />
    public async Task ReconfigureLogLevelAsync(string loggerName, LogLevelDto minLogLevel, LogLevelDto maxLogLevel)
    {
        var request = new RestRequest("diagnostics/reconfigureLogLevel", Method.Post);
        request.AddQueryParameter("loggerName", loggerName);
        request.AddQueryParameter("minLogLevel", minLogLevel);
        request.AddQueryParameter("maxLogLevel", maxLogLevel);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }
}