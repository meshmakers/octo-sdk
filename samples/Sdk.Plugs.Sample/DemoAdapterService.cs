using Meshmakers.Octo.Sdk.Common.Adapters;
using NLog;

namespace Sdk.Plugs.Sample;

public class DemoAdapterService : IAdapterService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public Task StartupAsync(AdapterStartup adapterStartup, CancellationToken stoppingToken)
    {
        try
        {
            Logger.Info("DemoPlugService started");
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
            Logger.Info("DemoPlugService stopped");
            return Task.CompletedTask;
        }
        catch (Exception e)
        {
            Logger.Error(e, "Error while shutdown");
            throw;
        }
    }
}