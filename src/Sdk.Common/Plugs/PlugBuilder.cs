using MassTransit;
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

namespace Meshmakers.Octo.Sdk.Common.Plugs;

/// <summary>
///     The plug builder is used to startup a plug.
/// </summary>
public class PlugBuilder
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    ///     Executes the startup of a plug.
    /// </summary>
    /// <param name="args">Program arguments</param>
    /// <param name="configureDelegate">A delegate to configure additional services</param>
    public void Run(string[] args, Action<HostBuilderContext, IServiceCollection> configureDelegate)
    {
        try
        {
            Logger.Info("Octo Mesh Plug, Version {ProductVersion}",
                AssemblyMetadataReader.GetProductVersion());
            Logger.Info("{Copyright}", AssemblyMetadataReader.GetCopyright());

            CreateHostBuilder(args, configureDelegate).Build().Run();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Stopped plug because of exception");
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
                services.Configure<PlugOptions>(options => builder.Configuration.GetSection("Plug").Bind(options));

                var startupOptions = new PlugOptions();
                builder.Configuration.GetSection("Plug").Bind(startupOptions);

                services.AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.SetMinimumLevel(LogLevel.Trace);
                    loggingBuilder.AddNLog("nlog.config");
                });

                if (startupOptions.UseBroker)
                {
                    services.AddMassTransit(x =>
                    {
                        x.UsingRabbitMq((context, cfg) =>
                        {
                            var plugOptions = context.GetService<IOptions<PlugOptions>>();
                            if (plugOptions == null)
                            {
                                throw new InvalidOperationException("PlugOptions not configured");
                            }

                            cfg.Host(plugOptions.Value.BrokerHost, plugOptions.Value.BrokerPort,
                                plugOptions.Value.BrokerVirtualHost, h =>
                                {
                                    h.Username(plugOptions.Value.BrokerUsername);
                                    h.Password(plugOptions.Value.BrokerPassword);
                                });
                            cfg.ConfigureEndpoints(context);
                        });
                    });
                }

                services.AddOptions<AdapterHubClientOptions>()
                    .Configure<IOptions<PlugOptions>>(
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

                services.AddHostedService<PlugExecutionService>();

                configureDelegate(builder, services);
            });
    }
}