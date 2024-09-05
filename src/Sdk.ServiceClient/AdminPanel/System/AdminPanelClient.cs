using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Meshmakers.Octo.Sdk.ServiceClient.AdminPanel.System;

/// <summary>
///     Implementation of the client proxy for asset services on system level.
/// </summary>
public class AdminPanelClient : ServiceClient, IAdminPanelClient
{
    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="serviceClientOptions">Options for configuration of the client proxy.</param>
    /// <param name="assetAccessToken">The access token management object</param>
    public AdminPanelClient(IOptions<AdminPanelClientOptions> serviceClientOptions,
        IAdminPanelClientAccessToken assetAccessToken)
        : this(serviceClientOptions.Value, assetAccessToken)
    {
    }

    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="serviceClientOptions">Options for configuration of the client proxy.</param>
    /// <param name="assetAccessToken">The access token management object</param>
    public AdminPanelClient(AdminPanelClientOptions serviceClientOptions,
        IAdminPanelClientAccessToken assetAccessToken)
        : base(serviceClientOptions, assetAccessToken)
    {
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

    /// <inheritdoc />
    protected override Uri BuildServiceUri()
    {
        if (string.IsNullOrWhiteSpace(Options.EndpointUri))
        {
            throw new ServiceConfigurationMissingException("Asset Repo Services URI is missing.");
        }

        return new Uri(Options.EndpointUri).Append("system").Append("v1");
    }
}