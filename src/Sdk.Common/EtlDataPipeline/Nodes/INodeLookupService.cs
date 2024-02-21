using System.Diagnostics.CodeAnalysis;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;

/// <summary>
/// Service for looking up pipeline nodes by their configuration qualified name.
/// </summary>
public interface INodeLookupService
{
    /// <summary>
    /// Tries to get the extract pipeline node by its configuration qualified name.
    /// </summary>
    /// <param name="nodeQualifiedName">Name of the node including version.</param>
    /// <param name="extractPipelineNode">The extract pipeline node.</param>
    /// <returns>True if the object pipeline node was found, otherwise false.</returns>
    bool TryGetExtractPipelineNode(string nodeQualifiedName, [NotNullWhen(true)] out IExtractPipelineNode? extractPipelineNode);
    
    /// <summary>
    /// Tries to get the transform pipeline node by its configuration qualified name.
    /// </summary>
    /// <param name="nodeQualifiedName">Name of the node including version.</param>
    /// <param name="transformPipelineNode">The transform pipeline node.</param>
    /// <returns>True if the object pipeline node was found, otherwise false.</returns>
    bool TryGetTransformPipelineNode(string nodeQualifiedName, [NotNullWhen(true)] out ITransformPipelineNode? transformPipelineNode);
    
    /// <summary>
    /// Tries to get the transform pipeline node by its configuration qualified name.
    /// </summary>
    /// <param name="nodeQualifiedName">Name of the node including version.</param>
    /// <param name="loadPipelineNode">The load pipeline node.</param>
    /// <returns>True if the object pipeline node was found, otherwise false.</returns>
    bool TryGetLoadPipelineNode(string nodeQualifiedName, [NotNullWhen(true)] out ILoadPipelineNode? loadPipelineNode);
    
    /// <summary>
    /// Tries to get the configuration node type by its configuration qualified name.
    /// </summary>
    /// <param name="nodeQualifiedName">Name of the node including version.</param>
    /// <param name="configurationNodeType">The configuration node type.</param>
    /// <returns>True if the object pipeline node was found, otherwise false.</returns>
    bool TryGetConfigurationNodeType(string nodeQualifiedName, [NotNullWhen(true)] out Type? configurationNodeType);
    
    /// <summary>
    /// Try to get the configuration qualified name of a node type.
    /// </summary>
    /// <param name="configurationNodeType">Configuration node type.</param>
    /// <param name="qualifiedName">The configuration qualified name.</param>
    /// <returns>True if the object pipeline node was found, otherwise false.</returns>
    bool TryGetNodeQualifiedName(Type configurationNodeType, [NotNullWhen(true)] out string? qualifiedName);

    /// <summary>
    /// Returns all node configuration types.
    /// </summary>
    /// <returns></returns>
    IEnumerable<Tuple<Type, string>> GetConfigurationNodeTypes();
}