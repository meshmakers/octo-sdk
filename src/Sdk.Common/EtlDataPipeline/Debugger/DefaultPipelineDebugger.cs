using System.Collections.Concurrent;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Debugger;

/// <summary>
/// Implements a default pipeline debugger
/// </summary>
public class DefaultPipelineDebugger : IPipelineDebugger
{
    private readonly DebugPipelineLogger _debugPipelineLogger;
    private readonly ConcurrentStack<DebugPoint> _debugPointStack = new();
    private readonly ConcurrentDictionary<NodePath, DebugPoint> _debugPoints = new();
    
    /// <summary>
    /// The pipeline runtime entity id
    /// </summary>
    // ReSharper disable once NotAccessedField.Global
    protected RtEntityId? PipelineRtEntityId;

    /// <summary>
    /// Creates a new instance of <see cref="T:Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Debugger.DefaultPipelineDebugger" />
    /// </summary>
    /// <param name="loggerFactory"></param>
    public DefaultPipelineDebugger(ILoggerFactory loggerFactory)
    {
        _debugPipelineLogger = new DebugPipelineLogger(loggerFactory);
        Logger = _debugPipelineLogger;
    }

    /// <inheritdoc />
    public IPipelineLogger Logger { get; }

    /// <inheritdoc />
    public void RegisterPipelineRtEntityId(RtEntityId pipelineRtEntityId)
    {
        PipelineRtEntityId = pipelineRtEntityId;
    }

    /// <inheritdoc />
    public void BeginPipelineExecution()
    {
        _debugPointStack.Clear();
        _debugPipelineLogger.Clear();
    }

    /// <inheritdoc />
    public virtual Task EndPipelineExecutionAsync()
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void LogInput(NodePath path, JToken? inputData, INodeConfiguration? nodeConfiguration)
    {
        var debugPoint = new DebugPoint(path, nodeConfiguration, inputData?.DeepClone());
        _debugPointStack.Push(debugPoint);
        _debugPoints.TryAdd(path, debugPoint);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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