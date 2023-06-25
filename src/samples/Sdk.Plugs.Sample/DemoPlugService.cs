using MassTransit;
using Meshmakers.Octo.Sdk.Common.Plugs;
using NLog;

namespace Sdk.Plugs.Sample;

public class DemoPlugService : IPlugService
{
    private readonly IPollingService _pollingService;
    private readonly IBus _bus;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public DemoPlugService(IPollingService pollingService, IBus bus)
    {
        _pollingService = pollingService;
        _bus = bus;
    }

    public Task StartupAsync(PlugStartup plugStartup, CancellationToken stoppingToken)
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