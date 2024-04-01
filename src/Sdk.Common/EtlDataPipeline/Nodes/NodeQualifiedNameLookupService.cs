using System.Diagnostics.CodeAnalysis;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;

internal class NodeQualifiedNameLookupService : INodeQualifiedNameLookupService
{
    private readonly Dictionary<string, Type> _byName;
    private readonly Dictionary<Type, string> _byConfigType;
    
    public NodeQualifiedNameLookupService(IDictionary<string, Type> nodeTypes)
    {
        _byName = new Dictionary<string, Type>(nodeTypes);
        _byConfigType = nodeTypes.ToDictionary(x => x.Value, x => x.Key);
    }

    public bool TryGetConfigurationNodeType(string nodeQualifiedName, [NotNullWhen(true)] out Type? nodeConfigurationType)
    {
        if (_byName.TryGetValue(nodeQualifiedName, out nodeConfigurationType))
        {
            return true;
        }

        nodeConfigurationType = null;
        return false;
    }

    public bool TryGetNodeConfigurationQualifiedName(Type configurationNodeType, [NotNullWhen(true)] out string? qualifiedName)
    {
        if (_byConfigType.TryGetValue(configurationNodeType, out qualifiedName))
        {
            return true;
        }

        qualifiedName = null;
        return false;
    }
}