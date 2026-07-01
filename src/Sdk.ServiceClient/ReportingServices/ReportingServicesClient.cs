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

        var reportingOptions = (ReportingServicesClientOptions)Options;
        if (string.IsNullOrWhiteSpace(reportingOptions.TenantId))
        {
            throw new ServiceConfigurationMissingException("Reporting services tenant ID is missing");
        }

        return new Uri(Options.EndpointUri).Append(reportingOptions.TenantId!).Append("v1");
    }

    /// <inheritdoc />
    public async Task EnableAsync(string tenantId)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);

        var request = new RestRequest("reporting/enable", Method.Post);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task DisableAsync(string tenantId)
    {
        ArgumentValidation.ValidateString(nameof(tenantId), tenantId);

        var request = new RestRequest("reporting/disable", Method.Post);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    public async Task ReconfigureLogLevelAsync(string loggerName, LogLevelDto minLogLevel, LogLevelDto maxLogLevel)
    {
        // The DiagnosticsController is system-scoped (system/v1/diagnostics). The client base URI is
        // tenant-scoped ({tenantId}/v1), so target the diagnostics endpoint via an absolute system URL
        // (RestSharp uses an absolute resource URL as-is instead of combining it with the base URI).
        var diagnosticsUri = new Uri(Options.EndpointUri!).Append("system", "v1", "diagnostics", "reconfigureLogLevel");
        var request = new RestRequest(diagnosticsUri, Method.Post);
        request.AddQueryParameter("loggerName", loggerName);
        request.AddQueryParameter("minLogLevel", minLogLevel);
        request.AddQueryParameter("maxLogLevel", maxLogLevel);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }
}