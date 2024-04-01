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
    void RegisterPipelineRtEntityId(RtEntityId pipelineRtEntityId);
    
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
    /// <param name="inputData">Input data before a node is processed</param>
    /// <param name="nodeConfiguration">The node configuration object</param>
    void LogInput(NodePath path, JToken? inputData, INodeConfiguration? nodeConfiguration);
    
    /// <summary>
    /// Logs the output of a node
    /// </summary>
    /// <param name="path">Path to the node</param>
    /// <param name="outputData">Output data after a node is processed</param>
    void LogOutput(NodePath path, JToken? outputData);

    /// <summary>
    /// Gets the debug information
    /// </summary>
    /// <returns></returns>
    DebugInformationRoot GetDebugInformation();
}