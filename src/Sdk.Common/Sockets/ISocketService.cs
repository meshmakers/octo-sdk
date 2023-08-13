namespace Meshmakers.Octo.Sdk.Common.Sockets;

/// <summary>
/// Interface for the socket service that allows to start and stop the socket service.
/// </summary>
/// <remarks>
/// This interface needs to be implemented by the socket assembly and registered in the DI container.
/// </remarks>
public interface ISocketService
{
    /// <summary>
    /// Gets called when the socket service should start.
    /// </summary>
    /// <param name="socketStartup">Startup configuration provided by configuration and backend</param>
    /// <param name="stoppingToken">The cancellation token to stop the operation of the socket</param>
    /// <returns></returns>
    Task StartupAsync(SocketStartup socketStartup, CancellationToken stoppingToken);
    
    /// <summary>
    /// Gets called when the socket service should stop.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token to stop the operation of the socket</param>
    /// <returns></returns>
    Task ShutdownAsync(CancellationToken stoppingToken);
}