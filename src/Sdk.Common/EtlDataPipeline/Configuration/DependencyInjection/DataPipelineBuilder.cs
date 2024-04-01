using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration.Serializer;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Debugger;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Loads;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration.DependencyInjection;

internal class DataPipelineBuilder : IDataPipelineBuilder
{
    private readonly List<NodeLookup> _nodeLookups = new();
    private readonly Dictionary<string, Type> _nodeConfigurations = new();

    public DataPipelineBuilder(IServiceCollection services)
    {
        Services = services;
        Services.AddScoped<IEtlContext>(s => s.GetRequiredService<IEtlContextAccessor<IEtlContext>>()
            .GetEtlContext());
    }

    public IServiceCollection Services { get; }
    
    
    public IDataPipelineBuilder RegisterNodeConfiguration(Type nodeConfigurationType)
    {
        var qualifiedName = nodeConfigurationType.GetConfigurationQualifiedName();
        if (!_nodeConfigurations.ContainsKey(qualifiedName))
        {
            _nodeConfigurations.Add(qualifiedName, nodeConfigurationType);
        }

        Services.TryAddSingleton<INodeQualifiedNameLookupService>(_ =>
            new NodeQualifiedNameLookupService(_nodeConfigurations));
        
        return this;
    }
    
    public IDataPipelineBuilder RegisterNodeConfiguration<TNodeType>() where TNodeType : INodeConfiguration
    {
        return RegisterNodeConfiguration(typeof(TNodeType));
    }

    public IDataPipelineBuilder RegisterNode(Type nodeType)
    {
        var configurationType = nodeType.GetNodeConfigurationType();
        var qualifiedName = configurationType.GetConfigurationQualifiedName();
        RegisterNodeConfiguration(configurationType);

        _nodeLookups.Add(new NodeLookup(qualifiedName, nodeType, configurationType));

        Services.TryAddSingleton<INodeLookupService>(_ => new NodeLookupService(_nodeLookups));

        return this;
    }

    public IDataPipelineBuilder RegisterNode<TNodeType>() where TNodeType : IPipelineNode
    {
        return RegisterNode(typeof(TNodeType));
    }

    public IDataPipelineBuilder RegisterEtlContext<TContext>() where TContext : class, IEtlContext
    {
        Services.AddScoped<TContext>(s => s.GetRequiredService<IEtlContextAccessor<TContext>>().GetEtlContext());
        return this;
    }
}