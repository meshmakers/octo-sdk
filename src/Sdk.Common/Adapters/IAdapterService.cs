namespace Meshmakers.Octo.Sdk.Common.Adapters;

/// <summary>
///     Interface for the adapter service that allows to start and stop a adapter
/// </summary>
/// <remarks>
///     This interface needs to be implemented by the adapter assembly and registered in the DI container.
/// </remarks>
public interface IAdapterService
{
    /// <summary>
    ///     Gets called when the adapter service should start.
    /// </summary>
    /// <param name="adapterStartup">Startup configuration provided by configuration and backend</param>
    /// <param name="stoppingToken">The cancellation token to stop the operation of the adapter</param>
    /// <returns></returns>
    Task StartupAsync(AdapterStartup adapterStartup, CancellationToken stoppingToken);

    /// <summary>
    ///     Gets called when the adapter service should stop.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token to stop the operation of the adapter</param>
    /// <returns></returns>
    Task ShutdownAsync(CancellationToken stoppingToken);
}