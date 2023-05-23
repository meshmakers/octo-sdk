using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Meshmakers.Octo.Common.Shared;
using Meshmakers.Octo.Communication.Plugs.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Client.PlugControllerServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NLog;

namespace Meshmakers.Octo.Sdk.PlugExecutor;

public abstract class PlugInWorkerBase : BackgroundService
{
    protected Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    protected IBus Bus { get; }
    protected IPlugControllerClient ControllerClient { get; }
    protected IOptions<PlugOptions> PlugOptions { get; }

    protected PlugConfigurationDto? Configuration { get; private set; }

    protected PlugInWorkerBase(IBus bus, IPlugControllerClient plugControllerClient,
        IOptions<PlugOptions> plugOptions)
    {
        Bus = bus;
        ControllerClient = plugControllerClient;
        PlugOptions = plugOptions;
    }

    protected async Task<bool> StartCommunicationAsync(CancellationToken stoppingToken)
    {
        Logger.Info("Starting plug...");
        Logger.Info("Connecting to Plug Hub at '{PlugControllerServicesUri}'",
            PlugOptions.Value.PlugControllerServicesUri);
        Logger.Info("TenantId '{TenantId}', plugId '{PlugId}'", 
            PlugOptions.Value.TenantId, PlugOptions.Value.PlugId);
        
        if (PlugOptions.Value.PlugId == null)
        {
            Logger.Error("PlugId is null");
            return false;
        }
        
        await ControllerClient.StartAsync();
        Logger.Info("Connected to plug hub");

        if (stoppingToken.IsCancellationRequested)
        {
            await ControllerClient.StopAsync();
            return false;
        }
        
        Logger.Info("Registering at plug hub");
        Configuration = await ControllerClient.RegisterPlugAsync(OctoObjectId.Parse(PlugOptions.Value.PlugId));
        Logger.Info("Registration successfull");

        return true;
    }
    
    protected async Task<bool> CheckCancellationToken(CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
        {
            await ControllerClient.StopAsync();
            return true;
        }

        return false;
    }
}