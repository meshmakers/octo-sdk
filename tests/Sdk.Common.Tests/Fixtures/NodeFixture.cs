using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration.Serializer;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sdk.Common.Tests.TestData;
using Sdk.Common.Tests.TestData.Dto;

namespace Sdk.Common.Tests.Fixtures;

public class NodeFixture : ServiceCollectionFixture
{
    private readonly List<NodeLookup> _nodeLookups = new();

    public NodeFixture()
    {
        RegisterNode(typeof(TestNode));
        RegisterNode(typeof(ExceptionNode));
        Services.TryAddSingleton<INodeLookupService>(_ => new NodeLookupService(_nodeLookups));
    }

    public Order OrderDto = Generator.GenerateOrder();


    public void RegisterNode(Type node)
    {
        var configurationType = node.GetNodeConfigurationType();
        var qualifiedName = configurationType.GetConfigurationQualifiedName();

        _nodeLookups.Add(new NodeLookup(qualifiedName, node, configurationType));
    }
}