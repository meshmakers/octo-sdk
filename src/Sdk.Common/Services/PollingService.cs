using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Meshmakers.Octo.Sdk.Common.Services;

/// <summary>
/// Implements the polling service that allows to add callbacks that get invoked at a specified interval
/// </summary>
public class PollingService : IPollingService
{
    private readonly HashSet<PollingItem> _callbacks;
    private Timer? _timer;

    /// <summary>
    /// Constructor
    /// </summary>
    public PollingService()
    {
        _callbacks = new HashSet<PollingItem>();
    }

    /// <inheritdoc />
    public void AddCallback(TimeSpan interval, Func<Task> callback)
    {
        _callbacks.Add(new PollingItem()
        {
            Action = callback,
            Interval = interval,
            LastExecutionTime = DateTime.MinValue
        });
    }

    /// <inheritdoc />
    public void Start()
    {
        _timer = new Timer(TimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    /// <inheritdoc />
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
                await pollingItem.Action();
            }
        }
    }
 
    private bool ShouldInvoke(PollingItem pollingItem)
    {
        var lastInvocationTime = pollingItem.LastExecutionTime;
        return DateTime.Now >= lastInvocationTime + pollingItem.Interval;
    }
}