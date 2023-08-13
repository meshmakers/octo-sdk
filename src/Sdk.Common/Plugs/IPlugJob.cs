namespace Meshmakers.Octo.Sdk.Common.Plugs;

/// <summary>
/// Interface of task/job based plugs.
/// </summary>
/// <remarks>
/// Use this interface for plugs that are executed in a low frequency, like once an hour, once a day or once a week.
/// This interface needs to be implemented by the plug assembly and registered in the DI container.
/// </remarks>
public interface IPlugJob
{
    /// <summary>
    /// Executed the task/job of the plug.
    /// </summary>
    /// <param name="plugStartup">Startup configuration provided by configuration and backend</param>
    /// <param name="stoppingToken">The cancellation token to stop the operation of the plug</param>
    /// <returns></returns>
    Task ExecuteAsync(PlugStartup plugStartup, CancellationToken stoppingToken);
}