using System;
using Meshmakers.Octo.Communication.Sockets.Contracts.Hubs;
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

public class SocketBuilder
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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

    private static IHostBuilder CreateHostBuilder(string[] args, Action<HostBuilderContext, IServiceCollection> configureDelegate) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureHostConfiguration(config => config.AddEnvironmentVariables(prefix: "OCTO_").AddCommandLine(args))
            .ConfigureServices((builder, services) =>
            {
                services.Configure<SocketOptions>(options => builder.Configuration.GetSection("Socket").Bind(options));

                services.AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.SetMinimumLevel(LogLevel.Trace);
                    loggingBuilder.AddNLog("nlog.config");
                });

                services.AddOptions<PlugHubClientOptions>()
                    .Configure<IOptions<SocketOptions>>(
                        (options, toolOptions) =>
                        {
                            options.TenantId = toolOptions.Value.TenantId;
                            options.PlugRtId = toolOptions.Value.SocketRtId;
                            options.EndpointUri = toolOptions.Value.CommunicationControllerServicesUri;
                        });

                services.AddSingleton<IServiceClientAccessToken, ServiceClientAccessToken>();

                services.AddSingleton<SocketHubCallbackService>();
                services.AddSingleton<ISocketHubCallbacks>(provider => provider.GetRequiredService<SocketHubCallbackService>());
                services.AddSingleton<ISocketHubCallbackService>(provider => provider.GetRequiredService<SocketHubCallbackService>());
                services.AddSingleton<ISocketHubClient, SocketHubClient>();

                services.AddHostedService<SocketExecutionService>();

                configureDelegate(builder, services);
            });
}