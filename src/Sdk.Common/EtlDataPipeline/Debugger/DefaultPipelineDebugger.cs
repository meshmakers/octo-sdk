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
        _debugPoints.AddOrUpdate(path, _ => new DebugPointDto(path, sequenceNumber)
        {
            Input = inputData == null ? null : JsonConvert.SerializeObject(inputData.DeepClone())
        }, (key, value) =>
        {
            value.Input = inputData == null ? null : JsonConvert.SerializeObject(inputData.DeepClone());
            return value;
        });
    }

    /// <inheritdoc />
    public void LogOutput(NodePath path, uint sequenceNumber, JToken? outputData)
    {
        _debugPoints.AddOrUpdate(path, _ => new DebugPointDto(path, sequenceNumber)
        {
            Output = outputData == null ? null : JsonConvert.SerializeObject(outputData.DeepClone())
        }, (key, value) =>
        {
            value.Output = outputData == null ? null : JsonConvert.SerializeObject(outputData.DeepClone());
            return value;
        });
    }

    /// <inheritdoc />
    public DebugInformationRoot GetDebugInformation()
    {
        foreach (var debugMessageGrouping in _debugPipelineLogger.Messages.GroupBy(x => x.NodePath))
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