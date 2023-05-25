using System;
using System.Linq;
using System.Reflection;
using MassTransit;
using Meshmakers.Octo.Sdk.Client.AssetRepositoryServices.Tenants;
using Meshmakers.Octo.Sdk.Client.PlugControllerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NLog;

namespace Meshmakers.Octo.Sdk.PlugExecutor;

public class PlugRunner
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public void Run(string[] args, Action<HostBuilderContext, IServiceCollection> configureDelegate)
    {
        try
        {
            Logger.Info("Octo Mesh Plug, Version {ProductVersion}",
                GetProductVersion());
            Logger.Info("{Copyright}", GetCopyright());

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

    private static string GetProductVersion()
    {
        var attribute = Assembly
            .GetExecutingAssembly()
            .GetCustomAttributes<AssemblyFileVersionAttribute>()
            .Single();
        return attribute.Version;
    }

    private static string GetCopyright()
    {
        var attribute = Assembly
            .GetExecutingAssembly()
            .GetCustomAttributes<AssemblyCopyrightAttribute>()
            .Single();

        return attribute.Copyright;
    }

    private static IHostBuilder CreateHostBuilder(string[] args, Action<HostBuilderContext, IServiceCollection> configureDelegate) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureHostConfiguration(config => config.AddEnvironmentVariables(prefix: "OCTO_").AddCommandLine(args))
            .ConfigureServices((builder, services) =>
            {
                services.Configure<PlugOptions>(options => builder.Configuration.GetSection("Plug").Bind(options));

                services.AddMassTransit(x =>
                {
                    x.UsingRabbitMq((context, cfg) =>
                    {
                        var plugOptions = context.GetService<IOptions<PlugOptions>>();
                        if (plugOptions == null)
                            throw new InvalidOperationException("PlugOptions not configured");
                        
                        cfg.Host(plugOptions.Value.BrokerHost, plugOptions.Value.BrokerPort,
                            plugOptions.Value.BrokerVirtualHost, h =>
                            {
                                h.Username(plugOptions.Value.BrokerUsername);
                                h.Password(plugOptions.Value.BrokerPassword);
                            });
                        cfg.ConfigureEndpoints(context);
                    });
                });

                services.AddOptions<PlugControllerClientOptions>()
                    .Configure<IOptions<PlugOptions>>(
                        (options, toolOptions) =>
                        {
                            options.TenantId = toolOptions.Value.TenantId;
                            options.EndpointUri = toolOptions.Value.PlugControllerServicesUri;
                        });

                services.AddSingleton<IPlugControllerServiceClientAccessToken, ServiceClientAccessToken>();

                services.AddSingleton<IPlugControllerClient, PlugControllerClient>();

                configureDelegate(builder, services);
            });
}