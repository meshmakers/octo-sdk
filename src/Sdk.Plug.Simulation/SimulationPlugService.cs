using Meshmakers.Octo.Sdk.Common.Plugs;
using NLog;

namespace Sdk.Plug.Simulation;

public class SimulationPlugService : IPlugService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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