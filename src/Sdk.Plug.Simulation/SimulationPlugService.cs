using Meshmakers.Octo.Sdk.Common.Plugs;
using Meshmakers.Octo.Sdk.Common.Services;
using NLog;

namespace Sdk.Plug.Simulation;

public class SimulationPlugService : IPlugService
{
    private readonly IPollingService _pollingService;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public SimulationPlugService(IPollingService pollingService)
    {
        _pollingService = pollingService;
    }

    public Task StartupAsync(PlugStartup plugStartup, CancellationToken stoppingToken)
    {
        try
        {
            Logger.Info("SimulationPlugService started");
            

            return Task.CompletedTask;
        }
        catch (Exception e)
        {
            Logger.Error(e, "Error while startup");
            throw;
        }
    }

    public Task ShutdownAsync(CancellationToken stoppingToken)
    {
        try
        {
            Logger.Info("SimulationPlugService stopped");
            return Task.CompletedTask;
        }
        catch (Exception e)
        {
            Logger.Error(e, "Error while shutdown");
            throw;
        }
    }
}