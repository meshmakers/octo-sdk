using System.Collections.Concurrent;

namespace Meshmakers.Octo.Sdk.Common.Services;

/// <summary>
///     Implements the polling service that allows to add callbacks that get invoked at a specified interval
/// </summary>
public class PollingService : IPollingService
{
    private readonly ConcurrentDictionary<PollingHandle, PollingItem> _callbacks;

    /// <summary>
    ///     Constructor
    /// </summary>
    public PollingService()
    {
        _callbacks = new();
    }

    /// <inheritdoc />
    public PollingHandle RegisterCallback(TimeSpan interval, Func<Task> callback)
    {
        var handle = new PollingHandle(this);
        var timer = new Timer(TimerCallback, handle, TimeSpan.Zero, interval);
        _callbacks.TryAdd(handle, new PollingItem
        {
            Action = callback,
            Interval = interval,
            Timer = timer,
            LastExecutionTime = DateTime.MinValue
        });

        return handle;
    }

    /// <inheritdoc />
    public void ClearCallbacks()
    {
        _callbacks.Clear();
    }

    /// <inheritdoc />
    public void UnregisterCallback(PollingHandle handle)
    {
        if (_callbacks.TryRemove(handle, out var pollingItem))
        {
            pollingItem.Timer?.Dispose();
        }
    }

    private async void TimerCallback(object? state)
    {
        if (state is not PollingHandle handle)
        {
            return;
        }

        if (_callbacks.TryGetValue(handle, out var pollingItem))
        {
            pollingItem.LastExecutionTime = DateTime.UtcNow;
            await pollingItem.Action();
        }
    }
}