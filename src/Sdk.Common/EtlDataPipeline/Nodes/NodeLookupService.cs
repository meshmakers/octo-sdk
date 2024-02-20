using System.Diagnostics.CodeAnalysis;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration.Serializer;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;

internal class NodeLookupService : INodeLookupService
{
    private readonly Dictionary<string, IExtractPipelineNode> _extractPipelineNodes;
    private readonly Dictionary<string, ITransformPipelineNode> _transformPipelineNodes;
    private readonly Dictionary<string, ILoadPipelineNode> _loadPipelineNodes;
    private readonly Dictionary<string, Type> _configurationNodeTypes;
    private readonly Dictionary<Type, string> _configurationNodeTypesByType;
    
    public NodeLookupService(IEnumerable<IExtractPipelineNode> extractPipelineNodes, IEnumerable<ITransformPipelineNode> transformPipelineNodes, IEnumerable<ILoadPipelineNode> loadPipelineNodes)
    {
        _extractPipelineNodes = extractPipelineNodes
            .ToDictionary(x => x.GetType().GetConfigurationQualifiedName(), x => x);
        _transformPipelineNodes = transformPipelineNodes
            .ToDictionary(x => x.GetType().GetConfigurationQualifiedName(), x => x);
        _loadPipelineNodes = loadPipelineNodes
            .ToDictionary(x => x.GetType().GetConfigurationQualifiedName(), x => x);
        
        // Ensure that the configuration can be loaded.
        var config = _extractPipelineNodes.Values
            .Union<IPipelineNode>(_transformPipelineNodes.Values)
            .Union(_loadPipelineNodes.Values).ToList();
        
        _configurationNodeTypes = config
            .ToDictionary(x => x.GetType().GetConfigurationQualifiedName(), x => x.GetType().GetConfigurationNodeType());
        _configurationNodeTypesByType = config
            .ToDictionary(x => x.GetType().GetConfigurationNodeType(), x => x.GetType().GetConfigurationQualifiedName());
    }
    
    public bool TryGetExtractPipelineNode(string nodeQualifiedName, [NotNullWhen(true)] out IExtractPipelineNode? extractPipelineNode)
    {
        return _extractPipelineNodes.TryGetValue(nodeQualifiedName, out extractPipelineNode);
    }

    public bool TryGetTransformPipelineNode(string nodeQualifiedName, [NotNullWhen(true)] out ITransformPipelineNode? transformPipelineNode)
    {
        return _transformPipelineNodes.TryGetValue(nodeQualifiedName, out transformPipelineNode);
    }

    public bool TryGetLoadPipelineNode(string nodeQualifiedName, [NotNullWhen(true)] out ILoadPipelineNode? loadPipelineNode)
    {
        return _loadPipelineNodes.TryGetValue(nodeQualifiedName, out loadPipelineNode);
    }

    public bool TryGetConfigurationNodeType(string nodeQualifiedName, [NotNullWhen(true)] out Type? configurationNodeType)
    {
        return _configurationNodeTypes.TryGetValue(nodeQualifiedName, out configurationNodeType);
    }

    public bool TryGetNodeQualifiedName(Type configurationNodeType, [NotNullWhen(true)] out string? qualifiedName)
    {
        return _configurationNodeTypesByType.TryGetValue(configurationNodeType, out qualifiedName);
    }
}