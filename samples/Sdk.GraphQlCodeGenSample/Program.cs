// See https://aka.ms/new-console-template for more information

using GraphQL;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;
using Meshmakers.Octo.Sdk.ServiceClient.Authentication;
using Meshmakers.Octo.Sdk.ServiceClient.IdentityServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sdk.GraphQlCodeGenSample;
using Sdk.GraphQlCodeGenSample.DataTransferObjects.System.v2;

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

// AB#8423: typed CK method clients. The sample model (ConstructionKit/, SdkSample) declares methods on Pump, so the
// generator emits RtPumpMethodClient next to the DTOs. Run with "methods <pumpRtId>" against a tenant that has the model.
if (args is ["methods", var pumpRtId])
{
    var pumps = new Sdk.GraphQlCodeGenSample.DataTransferObjects.SdkSample.v1.RtPumpMethodClient(tenantClient);

    // One idempotency key per operation; a retry of this start would pass the same key.
    var startKey = Guid.NewGuid().ToString("N");
    var started = await pumps.StartAsync(pumpRtId, targetRpm: 1500, idempotencyKey: startKey);
    if (started.IsSuccess)
    {
        // An accepted asynchronous call has an invocation id but no result yet.
        Console.WriteLine(started.Value is { } rpm
            ? $"Pump runs at {rpm} rpm (invocation {started.InvocationId})."
            : $"Start accepted, result pending (invocation {started.InvocationId}).");
    }
    else if (started.IsError(Sdk.GraphQlCodeGenSample.DataTransferObjects.SdkSample.v1.RtPumpMethodClient.StartErrors.PumpLocked))
    {
        // A declared error is a result, not an exception; authorization and validation errors throw CkMethodCallException.
        Console.WriteLine("The pump is locked for maintenance.");
    }

    var running = await pumps.CountRunningAsync();
    Console.WriteLine(running.Value is { } count
        ? $"Running pumps: {count}"
        : $"Count accepted, result pending (invocation {running.InvocationId}).");
}

// AB#8416: User method clients, generated in this project from System.Identity-[3.2,4.0) (construction-kits.yaml);
// they replace the obsolete REST methods IIdentityServicesClient.CreateUser/ResetPassword/AddRoleToUser/RemoveRoleFromUser.
// Run with "reset-password <userRtId>" against a tenant whose identity service offers the password methods on the bus
// (Methods:Identity:EnablePasswordMethods). The new password is read from the console without echo (or from redirected
// stdin), never from the command line.
if (args is ["reset-password", var userRtId])
{
    var newPassword = ReadSecret("New password: ");
    var users = new Sdk.GraphQlCodeGenSample.DataTransferObjects.System.Identity.v3.RtUserMethodClient(tenantClient);

    var reset = await users.ResetPasswordAsync(userRtId, newPassword, idempotencyKey: Guid.NewGuid().ToString("N"));
    if (reset.IsSuccess)
    {
        Console.WriteLine($"Password reset (invocation {reset.InvocationId}).");
    }
    else if (reset.IsError(Sdk.GraphQlCodeGenSample.DataTransferObjects.System.Identity.v3.RtUserMethodClient.ResetPasswordErrors
                 .PasswordPolicyViolation))
    {
        Console.WriteLine("The new password violates the password policy of the tenant.");
    }
    else
    {
        Console.WriteLine($"Password reset refused: {reset.Error!.Code}");
    }
}

// Reads a secret without echoing it; from redirected stdin it reads one line.
static string ReadSecret(string prompt)
{
    if (Console.IsInputRedirected)
    {
        return Console.ReadLine() ?? string.Empty;
    }

    Console.Write(prompt);
    var secret = new System.Text.StringBuilder();
    for (var key = Console.ReadKey(intercept: true); key.Key != ConsoleKey.Enter; key = Console.ReadKey(intercept: true))
    {
        if (key.Key == ConsoleKey.Backspace)
        {
            if (secret.Length > 0)
            {
                secret.Length--;
            }
        }
        else if (!char.IsControl(key.KeyChar))
        {
            secret.Append(key.KeyChar);
        }
    }

    Console.WriteLine();
    return secret.ToString();
}
