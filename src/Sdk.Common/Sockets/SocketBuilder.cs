using Meshmakers.Octo.Communication.Contracts.Hubs;
using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;
using Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog;
using NLog.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Meshmakers.Octo.Sdk.Common.Sockets;

/// <summary>
///     The plug builder is used to startup a socket.
/// </summary>
public class SocketBuilder
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    ///     Executes the startup of a socket.
    /// </summary>
    /// <param name="args">Program arguments</param>
    /// <param name="configureDelegate">A delegate to configure additional services</param>
    public void Run(string[] args, Action<HostBuilderContext, IServiceCollection> configureDelegate)
    {
        try
        {
            Logger.Info("Octo Mesh Socket, Version {ProductVersion}",
                AssemblyMetadataReader.GetProductVersion());
            Logger.Info("{Copyright}", AssemblyMetadataReader.GetCopyright());

            CreateHostBuilder(args, configureDelegate).Build().Run();
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

    private static IHostBuilder CreateHostBuilder(string[] args, Action<HostBuilderContext, IServiceCollection> configureDelegate)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureHostConfiguration(config => config.AddEnvironmentVariables("OCTO_").AddCommandLine(args))
            .ConfigureServices((builder, services) =>
            {
                services.Configure<SocketOptions>(options => builder.Configuration.GetSection("Socket").Bind(options));

                services.AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.SetMinimumLevel(LogLevel.Trace);
                    loggingBuilder.AddNLog("nlog.config");
                });

                services.AddOptions<AdapterHubClientOptions>()
                    .Configure<IOptions<SocketOptions>>(
                        (options, toolOptions) =>
                        {
                            options.TenantId = toolOptions.Value.TenantId;
                            options.AdapterRtId = toolOptions.Value.AdapterRtId;
                            options.EndpointUri = toolOptions.Value.CommunicationControllerServicesUri;
                        });

                services.AddSingleton<IServiceClientAccessToken, ServiceClientAccessToken>();

                services.AddSingleton<AdapterHubCallbackService>();
                services.AddSingleton<IAdapterHubCallbacks>(provider => provider.GetRequiredService<AdapterHubCallbackService>());
                services.AddSingleton<IAdapterHubCallbackService>(provider => provider.GetRequiredService<AdapterHubCallbackService>());
                services.AddSingleton<IAdapterHubClient, AdapterHubClient>();

                services.AddHostedService<SocketExecutionService>();

                configureDelegate(builder, services);
            });
    }
}