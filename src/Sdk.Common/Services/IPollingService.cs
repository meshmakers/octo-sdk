namespace Meshmakers.Octo.Sdk.Common.Services;

/// <summary>
///     Interface for the polling service that allows to add callbacks that get invoked at a specified interval
/// </summary>
public interface IPollingService
{
    /// <summary>
    ///     Adds a callback to be invoked at the specified interval
    /// </summary>
    /// <param name="interval">The interval the callback gets called.</param>
    /// <param name="callback">The function of the callback</param>
    void AddCallback(TimeSpan interval, Func<Task> callback);

    /// <summary>
    ///     Starts the polling process
    /// </summary>
    void Start();

    /// <summary>
    ///     Stops the polling process
    /// </summary>
    void Stop();
}