using System.Collections.Concurrent;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Debugger;

internal class DefaultPipelineDebugger : IPipelineDebugger
{
    private readonly DebugPipelineLogger _debugPipelineLogger;
    private readonly ConcurrentStack<DebugPoint> _debugPointStack = new();
    private readonly ConcurrentDictionary<NodePath, DebugPoint> _debugPoints = new();
    public DefaultPipelineDebugger(ILoggerFactory loggerFactory)
    {
        _debugPipelineLogger = new DebugPipelineLogger(loggerFactory);
        Logger = _debugPipelineLogger;
    }
    
    public event EventHandler<DebugEventArgs>? DebugPointReceived;
    public IPipelineLogger Logger { get; }
    
    public void BeginPipelineExecution()
    {
        _debugPointStack.Clear();
        _debugPipelineLogger.Clear();
    }

    public void EndPipelineExecution()
    {
        OnDebugPointReceived(new DebugEventArgs(GetDebugInformation()));
    }

    public void LogInput(NodePath path, JToken? inputData, INodeConfiguration? nodeConfiguration)
    {
        var debugPoint = new DebugPoint(path, nodeConfiguration, inputData?.DeepClone());
        _debugPointStack.Push(debugPoint);
        _debugPoints.TryAdd(path, debugPoint);
    }

    public void LogOutput(NodePath path, JToken? outputData)
    {
        if (_debugPoints.TryGetValue(path, out var debugPoint))
        {
            debugPoint.Output = outputData?.DeepClone();
        }
        else
        {
            throw new Exception("bad idea");
        }
    }

    protected virtual void OnDebugPointReceived(DebugEventArgs e)
    {
        DebugPointReceived?.Invoke(this, e);
    }

    public DebugInformationRoot GetDebugInformation()
    {
        foreach (var debugMessageGrouping in _debugPipelineLogger.Messages.GroupBy(x=> x.NodePath))
        {
            if (_debugPoints.TryGetValue(debugMessageGrouping.Key, out var debugPoint))
            {
                debugPoint.Messages = debugMessageGrouping.ToList();
            }
        }

        var debuggers = new DebugInformationRoot
        {
            DebugMessages = _debugPipelineLogger.Messages.ToList(),
            DebugPoints = _debugPoints.Values.ToList()
        };
        return debuggers;
    }
}