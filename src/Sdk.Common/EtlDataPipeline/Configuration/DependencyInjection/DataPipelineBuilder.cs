using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration.Serializer;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration.DependencyInjection;

internal class DataPipelineBuilder : IDataPipelineBuilder
{
    private readonly List<NodeLookup> _nodeLookups = new();

    public DataPipelineBuilder(IServiceCollection services)
    {
        Services = services;
        Services.AddScoped<IEtlContext>(s => s.GetRequiredService<IEtlContextAccessor<IEtlContext>>().GetEtlContext());
    }

    public IServiceCollection Services { get; }


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
    
    public IDataPipelineBuilder RegisterEtlContext<TContext>() where TContext : class, IEtlContext
    {
        Services.AddScoped<TContext>(s => s.GetRequiredService<IEtlContextAccessor<TContext>>().GetEtlContext());
        return this;
    }
}