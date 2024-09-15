using System.Collections.Concurrent;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Debugger;

/// <summary>
/// Implements a default pipeline debugger
/// </summary>
public class DefaultPipelineDebugger : IPipelineDebugger
{
    private readonly DebugPipelineLogger _debugPipelineLogger;
    private readonly ConcurrentStack<DebugPointDto> _debugPointStack = new();
    private readonly ConcurrentDictionary<NodePath, DebugPointDto> _debugPoints = new();
    
    /// <summary>
    /// The pipeline runtime entity id
    /// </summary>
    // ReSharper disable once NotAccessedField.Global
    protected RtEntityId? PipelineRtEntityId;
    
    /// <summary>
    /// The pipeline execution id, which is a guid that identifies the pipeline execution instance
    /// </summary>
    protected Guid? PipelineExecutionId;

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
    public void RegisterPipelineRtEntityId(RtEntityId pipelineRtEntityId, Guid pipelineExecutionId)
    {
        PipelineRtEntityId = pipelineRtEntityId;
        PipelineExecutionId = pipelineExecutionId;
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
    public void LogInput(NodePath path, uint sequenceNumber, JToken? inputData)
    {
        var debugPoint = new DebugPointDto(path, sequenceNumber, inputData == null ? null : JsonConvert.SerializeObject(inputData.DeepClone()));
        _debugPointStack.Push(debugPoint);
        _debugPoints.TryAdd(path, debugPoint);
    }

    /// <inheritdoc />
    public void LogOutput(NodePath path, JToken? outputData)
    {
        if (_debugPoints.TryGetValue(path, out var debugPoint))
        {
            debugPoint.Output = outputData == null ? null : JsonConvert.SerializeObject(outputData.DeepClone());
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
            PipelineRtEntityId = PipelineRtEntityId ?? throw new Exception("PipelineRtEntityId is not set"),
            PipelineExecutionId = PipelineExecutionId ?? throw new Exception("PipelineExecutionId is not set"),
            DebugPoints = _debugPoints.Values.ToList()
        };
        return debuggers;
    }
}