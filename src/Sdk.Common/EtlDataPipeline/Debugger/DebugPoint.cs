using System.Text.Json.Serialization;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Debugger;

/// <summary>
/// Represents a debug point with the before and after data and the node configuration
/// </summary>
public record DebugPoint
{
    /// <summary>
    /// Creates a new instance of <see cref="DebugPoint"/>
    /// </summary>
    /// <param name="input">Input data before execution node</param>
    /// <param name="nodeConfiguration">Node configuration</param>
    /// <param name="nodePath">Node path</param>
    [JsonConstructor]
    public DebugPoint( NodePath nodePath, INodeConfiguration? nodeConfiguration, JToken? input)
    {
        NodePath = nodePath;
        NodeConfiguration = nodeConfiguration;
        Input = input;
    }
    
    /// <summary>
    /// Gets the node path
    /// </summary>
    public NodePath NodePath { get; }
    
    /// <summary>
    /// Gets the node configuration
    /// </summary>
    public INodeConfiguration? NodeConfiguration { get; }
    
    /// <summary>
    /// Gets the input data
    /// </summary>
    public JToken? Input { get; }
    
    /// <summary>
    /// Gets or sets the output data
    /// </summary>
    public JToken? Output { get; set; }
    
    /// <summary>
    /// Gets or sets the debug messages
    /// </summary>
    public IEnumerable<DebugMessage>? Messages { get; set; }
}
