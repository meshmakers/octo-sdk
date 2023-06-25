using System.Threading;
using System.Threading.Tasks;

namespace Meshmakers.Octo.Sdk.Common.Plugs;

public interface IPlugService 
{
    Task StartupAsync(PlugStartup plugStartup, CancellationToken stoppingToken);
    Task ShutdownAsync(CancellationToken stoppingToken);
}