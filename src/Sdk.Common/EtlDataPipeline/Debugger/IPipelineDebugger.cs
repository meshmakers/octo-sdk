using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Debugger;

/// <summary>
/// Interface for the pipeline debugger
/// </summary>
public interface IPipelineDebugger
{
    /// <summary>
    /// Returns the debugging logger
    /// </summary>
    IPipelineLogger Logger { get; }

    /// <summary>
    /// Registers the pipeline runtime entity id
    /// </summary>
    /// <param name="pipelineRtEntityId">Entity id of the pipeline</param>
    /// <param name="pipelineExecutionId">Guid that identifies the pipeline execution instance</param>
    void RegisterPipelineRtEntityId(RtEntityId pipelineRtEntityId, Guid pipelineExecutionId);
    
    /// <summary>
    /// Signals the beginning of the pipeline execution
    /// </summary>
    void BeginPipelineExecution();
    
    /// <summary>
    /// Signals the end of the pipeline execution
    /// </summary>
    Task EndPipelineExecutionAsync();

    /// <summary>
    /// Logs the input of a node
    /// </summary>
    /// <param name="path">Path to the node</param>
    /// <param name="sequenceNumber">Sequence number of the node within a transformation list</param>
    /// <param name="inputData">Input data before a node is processed</param>
    void LogInput(NodePath path, uint sequenceNumber, JToken? inputData);
    
    /// <summary>
    /// Logs the output of a node
    /// </summary>
    /// <param name="path">Path to the node</param>
    /// <param name="sequenceNumber">Sequence number of the node within a transformation list</param>
    /// <param name="outputData">Output data after a node is processed</param>
    void LogOutput(NodePath path, uint sequenceNumber, JToken? outputData);

    /// <summary>
    /// Gets the debug information
    /// </summary>
    /// <returns></returns>
    DebugInformationRoot GetDebugInformation();
}