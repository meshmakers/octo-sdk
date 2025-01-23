// See https://aka.ms/new-console-template for more information

using GraphQL;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;
using Meshmakers.Octo.Sdk.ServiceClient.Authentication;
using Meshmakers.Octo.Sdk.ServiceClient.IdentityServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sdk.GraphQlCodeGenSample;
using Sdk.GraphQlCodeGenSample.DataTransferObjects.System.v1;

Console.WriteLine("Hello, World!");

var services = new ServiceCollection();
services.AddLogging(loggingBuilder =>
{
    loggingBuilder.SetMinimumLevel(LogLevel.Trace);
});

services.AddOptions<IdentityServiceClientOptions>()
    .Configure<IOptions<SampleOptions>>(
        (options, fireGuardiansOptions) => { options.EndpointUri = fireGuardiansOptions.Value.AuthorityUrl; });

services.AddOptions<AuthenticatorOptions>()
    .Configure<IOptions<SampleOptions>>(
        (options, fireGuardiansOptions) =>
        {
            options.IssuerUri = fireGuardiansOptions.Value.AuthorityUrl;
            options.ClientId = fireGuardiansOptions.Value.ClientId;
            options.ClientSecret = fireGuardiansOptions.Value.ClientSecret;
        });
    
services.AddOptions<TenantClientOptions>()
    .Configure<IOptions<SampleOptions>>(
        (options, backendOptions) =>
        {
            options.TenantId = backendOptions.Value.TenantId;
            options.EndpointUri = backendOptions.Value.AssetServiceUrl;
        });

services.AddTransient<IAuthenticationService, AuthenticationService>();
services.AddTransient<IAuthenticatorClient, AuthenticatorClient>();

services.AddTransient<ITenantClient, TenantClient>();
services.AddTransient<ITenantClientAccessToken, ServiceClientAccessToken>();


var serviceProvider = services.BuildServiceProvider();

var tenantClient = serviceProvider.GetRequiredService<ITenantClient>();
var query = new GraphQLRequest
{
    Query = GraphQl.GetTenantConfigurations,
    Variables = new { }
};
var itemsContainer = await tenantClient.SendQueryAsync<RtTenantConfigurationDto>(query);

if (itemsContainer?.Items != null)
{
    Console.WriteLine("Configuration:");
    
    foreach (var rtConfigurationDto in itemsContainer.Items)
    {
        Console.WriteLine(rtConfigurationDto.RtWellKnownName);
    }
}
