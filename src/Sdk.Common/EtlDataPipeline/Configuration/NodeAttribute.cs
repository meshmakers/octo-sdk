namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;

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
    /// <param name="nodeConfigurationType">Type of the node configuration</param>
    public NodeAttribute(string name, int version, Type nodeConfigurationType)
    {
        Name = name;
        Version = version;
        NodeConfigurationType = nodeConfigurationType;
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
    /// Type of node configuration
    /// </summary>
    public Type NodeConfigurationType { get; set; }
}