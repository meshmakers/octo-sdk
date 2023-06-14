using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Meshmakers.Octo.Sdk.PlugExecutor;

internal record PollingItem 
{
    public required Func<Task> Action { get; init; }
    public required TimeSpan Interval { get; init; }
    public required DateTime LastExecutionTime { get; set; }
}

public class PollingService : IPollingService
{
    private readonly HashSet<PollingItem> _callbacks;
    private Timer? _timer;

    public PollingService()
    {
        _callbacks = new HashSet<PollingItem>();
    }

    public void AddCallback(TimeSpan interval, Func<Task> callback)
    {
        _callbacks.Add(new PollingItem()
        {
            Action = callback,
            Interval = interval,
            LastExecutionTime = DateTime.MinValue
        });
    }

    public void Start()
    {
        _timer = new Timer(TimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private async void TimerCallback(object? state)
    {
        foreach (var pollingItem in _callbacks)
        {
            if (ShouldInvoke(pollingItem))
            {
                pollingItem.LastExecutionTime = DateTime.Now;
                await pollingItem.Action();//Task.Run(pollingItem.Action);
            }
        }
    }

    private bool ShouldInvoke(PollingItem pollingItem)
    {
        var lastInvocationTime = pollingItem.LastExecutionTime;
        return DateTime.Now >= lastInvocationTime + pollingItem.Interval;
    }

}