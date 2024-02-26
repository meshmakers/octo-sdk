using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration.Serializer;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration.DependencyInjection;

internal class DataPipelineBuilder(IServiceCollection services) : IDataPipelineBuilder
{
    private readonly List<NodeLookup> _nodeLookups = new();

    public IServiceCollection Services { get; } = services;


    public IDataPipelineBuilder RegisterNode(Type nodeType)
    {
        var qualifiedName = nodeType.GetConfigurationQualifiedName();
        var configurationType = nodeType.GetNodeConfigurationType();
        
        _nodeLookups.Add(new NodeLookup(qualifiedName, nodeType, configurationType));

        Services.TryAddSingleton<INodeLookupService>(_ => new NodeLookupService(_nodeLookups));

        return this;
    }

    public IDataPipelineBuilder RegisterNode<TNodeType>() where TNodeType : IPipelineNode
    {
        return RegisterNode(typeof(TNodeType));
    }
}