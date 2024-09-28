using System.Collections.Concurrent;
using MassTransit.Monitoring.Performance;
using Meshmakers.Common.Shared;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Microsoft.Extensions.Logging;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Debugger;

internal class DebugPipelineLogger(ILoggerFactory loggerFactory)
    : DefaultPipelineLogger(loggerFactory.CreateLogger<DefaultPipelineLogger>())
{
    public ConcurrentQueue<DebugMessage> Messages { get; } = new();

    public void Clear()
    {
#if !NETSTANDARD2_0
        Messages.Clear();
#else
        while (Messages.TryDequeue(out _))
        {
        }
#endif
    }

    public override void Debug(string nodePath, string message, params object[] args)
    {
        base.Debug(nodePath, message, args);
        Messages.Enqueue(new DebugMessage(LoggerSeverity.Debug, nodePath, GetMessage(message, args), DateTime.UtcNow));
    }

    public override void Info(string nodePath, string message, params object[] args)
    {
        base.Info(nodePath, message, args);
        Messages.Enqueue(new DebugMessage(LoggerSeverity.Information, nodePath, GetMessage(message, args), DateTime.UtcNow));
    }

    public override void Warning(string nodePath, string message, params object[] args)
    {
        base.Warning(nodePath, message, args);
        Messages.Enqueue(new DebugMessage(LoggerSeverity.Warning, nodePath, GetMessage(message, args), DateTime.UtcNow));
    }

    public override void Error(string nodePath, string message, params object[] args)
    {
        base.Error(nodePath, message, args);
        Messages.Enqueue(new DebugMessage(LoggerSeverity.Error, nodePath, GetMessage(message, args), DateTime.UtcNow));
    }

    public override void Error(string nodePath, Exception exception, string message, params object[] args)
    {
        base.Error(nodePath, exception, message, args);

        string exceptionMessage = exception.GetDirectAndIndirectMessages();
        Messages.Enqueue(new DebugMessage(LoggerSeverity.Error, nodePath, GetMessage(message, args), DateTime.Now, exceptionMessage));
    }
    
    private static string GetMessage(string message, object[] args)
    {
        return args.Length == 0 ? message : string.Format(message, args);
    }
}
