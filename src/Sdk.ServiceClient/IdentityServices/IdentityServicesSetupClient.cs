using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Meshmakers.Octo.Sdk.ServiceClient.IdentityServices;

/// <summary>
///     Client for the identity services setup.
/// </summary>
public class IdentityServicesSetupClient : ServiceClient, IIdentityServicesSetupClient
{
    /// <summary>
    /// </summary>
    /// <param name="serviceClientOptions">Options for configuration of the client proxy.</param>
    public IdentityServicesSetupClient(IOptions<IdentityServiceClientOptions> serviceClientOptions)
        : this(serviceClientOptions.Value)
    {
    }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="serviceClientOptions">Options for configuration of the client proxy.</param>
    public IdentityServicesSetupClient(IdentityServiceClientOptions serviceClientOptions)
        : base(serviceClientOptions)
    {
    }

    /// <inheritdoc />
    public async Task AddAdminUser(AdminUserDto adminUserDto)
    {
        var request = new RestRequest("setup", Method.Post);
        request.AddJsonBody(adminUserDto);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    /// <inheritdoc />
    protected override Uri BuildServiceUri()
    {
        if (string.IsNullOrWhiteSpace(Options.EndpointUri))
        {
            throw new ServiceConfigurationMissingException("Identity services URI is missing.");
        }

        return new Uri(Options.EndpointUri).Append("system").Append("v1");
    }
}