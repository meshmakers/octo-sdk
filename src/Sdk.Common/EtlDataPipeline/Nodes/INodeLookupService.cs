using System.Diagnostics.CodeAnalysis;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;

/// <summary>
/// Service for looking up pipeline nodes by their configuration qualified name.
/// </summary>
public interface INodeLookupService
{
    /// <summary>
    /// Trie to create an instance of a pipeline node by its configuration qualified name.
    /// </summary>
    /// <param name="nodeQualifiedName">Name of the node including version.</param>
    /// <param name="next">Delegate to the next node in the pipeline.</param>
    /// <param name="pipelineNode">The pipeline node.</param>
    /// <returns>The pipeline node if it was found and instance is created, otherwise null.</returns>
    bool TryCreateInstance(string nodeQualifiedName, NodeDelegate next, [NotNullWhen(true)] out IPipelineNode? pipelineNode);
    
    /// <summary>
    /// Try to get the configuration qualified name of a node type.
    /// </summary>
    /// <param name="configurationNodeType">Configuration node type.</param>
    /// <param name="qualifiedName">The configuration qualified name.</param>
    /// <returns>True if the object pipeline node was found, otherwise false.</returns>
    bool TryGetNodeConfigurationQualifiedName(Type configurationNodeType, [NotNullWhen(true)] out string? qualifiedName);
    
    /// <summary>
    /// Try to get the configuration node type by its qualified name.
    /// </summary>
    /// <param name="nodeQualifiedName">The configuration qualified name.</param>
    /// <param name="nodeConfigurationType">Configuration node type.</param>
    /// <returns>True if the object pipeline node was found, otherwise false.</returns>
    bool TryGetConfigurationNodeType(string nodeQualifiedName, [NotNullWhen(true)] out Type? nodeConfigurationType);

    /// <summary>
    /// Returns all node configuration types.
    /// </summary>
    /// <returns></returns>
    IEnumerable<Tuple<Type, string>> GetConfigurationNodeTypes();
}