namespace Meshmakers.Octo.Sdk.Common.DataPipeline.Configuration;

/// <summary>
/// Describes a node in the pipeline
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class NodeAttribute : Attribute
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="name">Name of the node</param>
    /// <param name="version">Version of the node</param>
    /// <param name="nodeType">Type of the node</param>
    public NodeAttribute(string name, int version, Type nodeType)
    {
        Name = name;
        Version = version;
        NodeType = nodeType;
    }

    /// <summary>
    /// Returns the name of the node
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Describes the version of the node
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Description of the node type
    /// </summary>
    public Type NodeType { get; set; }
}