namespace Meshmakers.Octo.Sdk.Common.Plugs;

/// <summary>
/// Interface for the plug service that allows to start and stop a plug
/// </summary>
/// <remarks>
/// This interface needs to be implemented by the plug assembly and registered in the DI container.
/// </remarks> 
public interface IPlugService 
{
    /// <summary>
    /// Gets called when the plug service should start.
    /// </summary>
    /// <param name="plugStartup">Startup configuration provided by configuration and backend</param>
    /// <param name="stoppingToken">The cancellation token to stop the operation of the plug</param>
    /// <returns></returns>
    Task StartupAsync(PlugStartup plugStartup, CancellationToken stoppingToken);
    
    /// <summary>
    /// Gets called when the plug service should stop.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token to stop the operation of the plug</param>
    /// <returns></returns>
    Task ShutdownAsync(CancellationToken stoppingToken);
}