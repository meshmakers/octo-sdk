using Meshmakers.Octo.Communication.Contracts.Hubs;
using Meshmakers.Octo.Sdk.Common.Adapters;
using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;
using Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;
using Microsoft.Extensions.Options;
using NLog;
using NLog.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Meshmakers.Octo.Sdk.Common.Web.Sockets;

/// <summary>
///     The plug builder is used to startup a socket.
/// </summary>
public class WebSocketBuilder
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    ///     Executes the startup of a socket.
    /// </summary>
    /// <param name="args">Program arguments</param>
    /// <param name="configureServicesDelegate">A delegate to configure additional services</param>
    /// <param name="configureApp">A delegate to configure apps</param>
    public async Task RunAsync(string[] args, Action<IServiceCollection> configureServicesDelegate, Action<WebApplication> configureApp)
    {
        try
        {
            Logger.Info("Octo Mesh Socket, Version {ProductVersion}",
                AssemblyMetadataReader.GetProductVersion());
            Logger.Info("{Copyright}", AssemblyMetadataReader.GetCopyright());

            var builder = CreateHostBuilder(args, configureServicesDelegate);
            var app = builder.Build();
            configureApp(app);

            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Stopped socket because of exception");
        }
        finally
        {
            // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
            LogManager.Shutdown();
        }
    }

    private static WebApplicationBuilder CreateHostBuilder(string[] args, Action<IServiceCollection> configureServicesDelegate)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddEnvironmentVariables("OCTO_").AddCommandLine(args);
        builder.Services.Configure<AdapterOptions>(options => builder.Configuration.GetSection("Socket").Bind(options));

        builder.Services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.SetMinimumLevel(LogLevel.Trace);
            loggingBuilder.AddNLog("nlog.config");
        });

        builder.Services.AddOptions<AdapterHubClientOptions>()
            .Configure<IOptions<AdapterOptions>>(
                (options, socketOptions) =>
                {
                    options.TenantId = socketOptions.Value.TenantId;
                    options.AdapterRtId = socketOptions.Value.AdapterRtId;
                    options.EndpointUri = socketOptions.Value.CommunicationControllerServicesUri;
                });

        builder.Services.AddSingleton<IServiceClientAccessToken, ServiceClientAccessToken>();

        builder.Services.AddSingleton<AdapterHubCallbackService>();
        builder.Services.AddSingleton<IAdapterHubCallbacks>(provider => provider.GetRequiredService<AdapterHubCallbackService>());
        builder.Services.AddSingleton<IAdapterHubCallbackService>(provider => provider.GetRequiredService<AdapterHubCallbackService>());
        builder.Services.AddSingleton<IAdapterHubClient, AdapterHubClient>();

        builder.Services.AddHostedService<AdapterExecutionService>();

        configureServicesDelegate(builder.Services);

        return builder;
    }
}