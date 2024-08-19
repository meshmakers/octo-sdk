using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Buffering;

internal interface IBufferScheduler
{
    void ScheduleOrReplace(Func<Task> action, TimeSpan delay);
    Task StopAsync();
    Task StartAsync(CancellationToken cancellationToken);
}

internal class BufferScheduler(ILogger<BufferScheduler> logger) : IBufferScheduler
{
    private bool _shouldStop;
    private readonly Stack<ExecutionItem> _tasks = [];
    
    public void ScheduleOrReplace(Func<Task> action, TimeSpan delay)
    {
        if (_tasks.Count != 0)
        {
            _ = _tasks.Pop();
        }

        _tasks.Push(new ExecutionItem(action, delay));
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var t = new Thread(Start);
        t.Start(cancellationToken);

        return Task.CompletedTask;
    }

    private void Start(object? cancellationToken)
    {
        if (cancellationToken is not CancellationToken token)
        {
            logger.LogWarning("Got not cancellation token -> dont start.");
            return;
        }
        
        _shouldStop = false;
        while (!_shouldStop && !token.IsCancellationRequested)
        {
            var tasksToRun = _tasks.Where(task => DateTimeOffset.UtcNow - task.LastExecution >= task.Delay).ToList();
            
            logger.LogDebug("Found {Count} tasks to run", tasksToRun.Count);
            
            foreach (var task in tasksToRun)
            {
                task.Action().ConfigureAwait(false).GetAwaiter().GetResult();
                task.LastExecution = DateTimeOffset.UtcNow;
            }

            Thread.Sleep(1_000);
        }
    }

    public Task StopAsync()
    {
        _shouldStop = true;
        return Task.CompletedTask;
    }

    private class ExecutionItem(Func<Task> action, TimeSpan delay)
    {
        public Func<Task> Action { get; } = action;
        public TimeSpan Delay { get; } = delay;
        public DateTimeOffset LastExecution { get; set; } = DateTimeOffset.MinValue;
    }
}

/// <summary>
/// 
/// </summary>
internal class BufferSchedulerHostedService : IHostedService
{
    private readonly IBufferScheduler _scheduler;

    public BufferSchedulerHostedService(IBufferScheduler scheduler)
    {
        _scheduler = scheduler;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _scheduler.StartAsync(cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _scheduler.StopAsync();
        return Task.CompletedTask;
    }
}