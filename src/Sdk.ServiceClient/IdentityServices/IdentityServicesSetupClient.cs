using System;
using System.Threading.Tasks;
using Meshmakers.Octo.Common.Shared;
using Meshmakers.Octo.Common.Shared.DataTransferObjects;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Meshmakers.Octo.Sdk.ServiceClient.IdentityServices;

public class IdentityServicesSetupClient : ServiceClient, IIdentityServicesSetupClient
{
    public IdentityServicesSetupClient(IOptions<IdentityServiceClientOptions> identityServiceClientOptions)
        : this(identityServiceClientOptions.Value)
    {
    }

    public IdentityServicesSetupClient(IdentityServiceClientOptions identityServiceClientOptions)
        : base(identityServiceClientOptions)
    {
    }

    public async Task AddAdminUser(AdminUserDto adminUserDto)
    {
        var request = new RestRequest("setup", Method.Post);
        request.AddJsonBody(adminUserDto);

        var response = await Client.ExecuteAsync(request);
        ValidateResponse(response);
    }

    protected override Uri BuildServiceUri()
    {
        if (string.IsNullOrWhiteSpace(Options.EndpointUri))
        {
            throw new ServiceConfigurationMissingException("Identity services URI is missing.");
        }

        return new Uri(Options.EndpointUri).Append("system").Append("v1");
    }
}
