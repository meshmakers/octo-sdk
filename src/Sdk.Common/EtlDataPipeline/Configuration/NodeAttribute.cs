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
    /// <param name="configurationNodeType">Type of the config node</param>
    public NodeAttribute(string name, int version, Type configurationNodeType)
    {
        Name = name;
        Version = version;
        ConfigurationNodeType = configurationNodeType;
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
    public Type ConfigurationNodeType { get; set; }
}