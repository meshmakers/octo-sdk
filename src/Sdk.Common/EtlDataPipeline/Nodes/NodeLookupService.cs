using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;

internal class NodeLookupService : INodeLookupService
{
    private readonly Dictionary<string, NodeLookup> _byName;
    private readonly Dictionary<Type, NodeLookup> _byConfigType;
    
    public NodeLookupService(IEnumerable<NodeLookup> nodeTypes)
    {
        var nodeTypeList = nodeTypes.ToList();
        
        _byName = nodeTypeList.ToDictionary(x => x.QualifiedName, x => x);
        _byConfigType = nodeTypeList.ToDictionary(x => x.NodeConfigurationType, x => x);
    }

    public bool TryCreateInstance(IServiceProvider services, string nodeQualifiedName, NodeDelegate next, [NotNullWhen(true)] out IPipelineNode? pipelineNode)
    {
        if (_byName.TryGetValue(nodeQualifiedName, out var nodeLookup))
        {
            pipelineNode = (IPipelineNode?)ActivatorUtilities.CreateInstance(services, nodeLookup.NodeType, next);
            if (pipelineNode == null)
            {
                throw DataPipelineException.CannotCreateInstance(nodeLookup.NodeType);
            }
            return true;
        }

        pipelineNode = null;
        return false;
    }

    public bool TryGetNodeConfigurationTypeQualifiedName(Type configurationNodeType, out string? qualifiedName)
    {
        throw new NotImplementedException();
    }

    public bool TryGetConfigurationNodeType(string nodeQualifiedName, [NotNullWhen(true)] out Type? nodeConfigurationType)
    {
        if (_byName.TryGetValue(nodeQualifiedName, out var nodeLookup))
        {
            nodeConfigurationType = nodeLookup.NodeConfigurationType;
            return true;
        }

        nodeConfigurationType = null;
        return false;
    }

    public bool TryGetNodeConfigurationQualifiedName(Type configurationNodeType, [NotNullWhen(true)] out string? qualifiedName)
    {
        if (_byConfigType.TryGetValue(configurationNodeType, out var nodeLookup))
        {
            qualifiedName = nodeLookup.QualifiedName;
            return true;
        }

        qualifiedName = null;
        return false;
    }

    public IEnumerable<Tuple<Type, string>> GetConfigurationNodeTypes()
    {
        return _byName.Select(x => new Tuple<Type, string>(x.Value.NodeConfigurationType, x.Key));
    }
}