using FireGuardians.Services;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;
using Meshmakers.Octo.Sdk.ServiceClient.Authentication;
using Meshmakers.Octo.Sdk.ServiceClient.IdentityServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace Sdk.ServiceClient.SystemTests.Fixtures;

public class ServiceCollectionFixture
{
    public ServiceCollectionFixture()
    {
        Services = new ServiceCollection();
        Services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.SetMinimumLevel(LogLevel.Trace);
        });
    }

    public ServiceCollection Services { get; }
    
    public void UseXUnitLoggerFactory(ITestOutputHelper testOutputHelper)
    {
        Services.AddSingleton<ITestOutputHelper>(sp => testOutputHelper);
        Services.AddSingleton<ILoggerFactory, XUnitLoggerFactory>();
    }
    
    public void UseRepositoryClient()
    {
        Services.AddOptions<IdentityServiceClientOptions>()
            .Configure<IOptions<FireGuardiansOptions>>(
                (options, fireGuardiansOptions) => { options.EndpointUri = fireGuardiansOptions.Value.AuthorityUrl; });

        Services.AddOptions<AuthenticatorOptions>()
            .Configure<IOptions<FireGuardiansOptions>>(
                (options, fireGuardiansOptions) =>
                {
                    options.IssuerUri = fireGuardiansOptions.Value.AuthorityUrl;
                    options.ClientId = fireGuardiansOptions.Value.ClientId;
                    options.ClientSecret = fireGuardiansOptions.Value.ClientSecret;
                });
    
        Services.AddOptions<TenantClientOptions>()
            .Configure<IOptions<FireGuardiansOptions>>(
                (options, backendOptions) =>
                {
                    options.TenantId = backendOptions.Value.TenantId;
                    options.EndpointUri = backendOptions.Value.AssetServiceUrl;
                });

        Services.AddTransient<IAuthenticationService, AuthenticationService>();
        Services.AddTransient<IAuthenticatorClient, AuthenticatorClient>();

        Services.AddTransient<ITenantClient, TenantClient>();
        Services.AddTransient<ITenantClientAccessToken, ServiceClientAccessToken>();
    }
}