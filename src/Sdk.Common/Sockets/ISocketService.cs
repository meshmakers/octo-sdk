using System.Threading;
using System.Threading.Tasks;

namespace Meshmakers.Octo.Sdk.Common.Sockets;

public interface ISocketService
{
    Task StartupAsync(SocketStartup socketStartup, CancellationToken stoppingToken);
    Task ShutdownAsync(CancellationToken stoppingToken);
}