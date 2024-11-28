using System.Net;
using Meshmakers.Octo.Common.DistributionEventHub.Configuration;
using Meshmakers.Octo.Communication.Contracts.Hubs;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Debugger;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Buffering.EdgeBuffer;
using Meshmakers.Octo.Sdk.Common.Services;
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

namespace Meshmakers.Octo.Sdk.Common.Adapters;

/// <summary>
///     The adapter builder is used to start up an adapter.
/// </summary>
public class AdapterBuilder
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    ///     Executes the startup of an adapter.
    /// </summary>
    /// <param name="args">Program arguments</param>
    /// <param name="postConfigureDelegate">A delegate to configure additional services after the SDK itself has been initialized.</param>
    /// <param name="configureDistributionEventHub">Configuration of the distribution event hub</param>
    public void Run(string[] args, Action<HostBuilderContext, IServiceCollection> postConfigureDelegate,
        Action<IDistributionEventHubConfiguration>? configureDistributionEventHub = null)
    {
        Run(args, null, postConfigureDelegate, configureDistributionEventHub);
    }

    /// <summary>
    ///     Executes the startup of an adapter.
    /// </summary>
    /// <param name="args">Program arguments</param>
    /// <param name="preConfigureDelegate">A delegate to configure additional services before the SDK itself has been initialized.</param>
    /// <param name="postConfigureDelegate">A delegate to configure additional services after the SDK itself has been initialized.</param>
    /// <param name="configureDistributionEventHub">Configuration of the distribution event hub</param>
    // ReSharper disable once MemberCanBePrivate.Global
    public void Run(string[] args, Action<HostBuilderContext, IServiceCollection>? preConfigureDelegate, Action<HostBuilderContext, IServiceCollection> postConfigureDelegate,
        Action<IDistributionEventHubConfiguration>? configureDistributionEventHub = null)
    {
        try
        {
            Logger.Info($"Octo Mesh Adapter, Version {AssemblyMetadataReader.GetProductVersion()}");
            Logger.Info(AssemblyMetadataReader.GetCopyright());

            CreateHostBuilder(args, preConfigureDelegate, postConfigureDelegate, configureDistributionEventHub).Build().Run();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Stopped adapter because of exception");
        }
        finally
        {
            // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
            LogManager.Shutdown();
        }
    }

    /// <summary>
    /// Stops the adapter gracefully.
    /// </summary>
    public void Stop()
    {
        AdapterLifetimeManagement.Instance?.Stop();
    }

    private static IHostBuilder CreateHostBuilder(string[] args,
        Action<HostBuilderContext, IServiceCollection>? preConfigureDelegate,
        Action<HostBuilderContext, IServiceCollection> postConfigureDelegate,
        Action<IDistributionEventHubConfiguration>? configureDistributionEventHub)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureHostConfiguration(config => config.AddEnvironmentVariables("OCTO_").AddCommandLine(args))
            .ConfigureServices((builder, services) =>
            {
                if (preConfigureDelegate != null)
                {
                    preConfigureDelegate(builder, services);
                }
                
                services.AddSingleton<AdapterLifetimeManagement>();
                services.Configure<AdapterOptions>(options =>
                    builder.Configuration.GetSection("Adapter").Bind(options));

                var startupOptions = new AdapterOptions();
                builder.Configuration.GetSection("Adapter").Bind(startupOptions);

                services.Configure<EdgeDataBufferConfiguration>(options =>
                    builder.Configuration.GetSection("EdgeDataBuffer").Bind(options));

                services.AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.SetMinimumLevel(LogLevel.Trace);
                    loggingBuilder.AddNLog(startupOptions.NlogConfigPath);
                });

                if (startupOptions.UseBroker)
                {
                    services.AddDistributionEventHubWithOptions(s =>
                    {
                        s.BrokerHost = startupOptions.BrokerHost;
                        s.BrokerPort = startupOptions.BrokerPort;
                        s.BrokerUser = startupOptions.BrokerUsername;
                        s.BrokerPassword = startupOptions.BrokerPassword;
                    }, c =>
                    {
                        c.AutomaticallyStartBusDuringStartup = false;
                        c.UniqueServiceAddress = $"adapter_{startupOptions.AdapterRtId}";
                        configureDistributionEventHub?.Invoke(c);
                    });
                }

                if (startupOptions.IgnoreCertificateValidation)
                {
                    ServicePointManager.ServerCertificateValidationCallback += (_, _, _, _) => true;
                }

                services.AddOptions<AdapterHubClientOptions>()
                    .Configure<IOptions<AdapterOptions>>(
                        (options, toolOptions) =>
                        {
                            options.TenantId = toolOptions.Value.TenantId;
                            options.AdapterRtId = toolOptions.Value.AdapterRtId;
                            options.AdapterCkTypeId = toolOptions.Value.AdapterCkTypeId;
                            options.EndpointUri = toolOptions.Value.CommunicationControllerServicesUri;
                        });

                services.AddSingleton<IPipelineRegistryService, PipelineRegistryService>();
                services.AddSingleton<IServiceClientAccessToken, ServiceClientAccessToken>();

                services.AddSingleton<AdapterHubCallbackService>();
                services.AddSingleton<IAdapterHubCallbacks>(provider =>
                    provider.GetRequiredService<AdapterHubCallbackService>());
                services.AddSingleton<IAdapterHubCallbackService>(provider =>
                    provider.GetRequiredService<AdapterHubCallbackService>());
                services.AddSingleton<IAdapterHubClient, AdapterHubClient>();
                services.AddTransient<IPipelineDebugger, AdapterPipelineDebugger>();

                services.AddHostedService<AdapterExecutionService>();

                postConfigureDelegate(builder, services);
            });
    }
}