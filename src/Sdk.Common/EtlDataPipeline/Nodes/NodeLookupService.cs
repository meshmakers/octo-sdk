using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;

internal class NodeLookupService : INodeLookupService
{
    private readonly Dictionary<string, NodeLookup> _byName;
    private readonly Dictionary<Type, NodeLookup> _byConfigType;
    
    public NodeLookupService(List<NodeLookup> nodeLookups)
    {
        _byName = nodeLookups.ToDictionary(x => x.QualifiedName, x => x);
        _byConfigType = nodeLookups.ToDictionary(x => x.NodeConfigurationType, x => x);
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
}