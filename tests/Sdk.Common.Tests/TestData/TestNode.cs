using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;

namespace Sdk.Common.Tests.TestData;

[NodeName("Test", 1)]
internal record TestNodeConfiguration : TargetPathNodeConfiguration;

[NodeConfiguration(typeof(TestNodeConfiguration))]
internal class TestNode(NodeDelegate next, ITestCounter testCounter) : IPipelineNode
{
    /// <inheritdoc />
    public async Task ProcessObjectAsync(IDataContext dataContext, INodeContext nodeContext)
    {
        var c = nodeContext.GetNodeConfiguration<TestNodeConfiguration>();

        dataContext.SetValueByPath(c.TargetPath, c.DocumentMode, c.TargetValueKind, c.TargetValueWriteMode, testCounter.GetNext());
          
        await next(dataContext, nodeContext);
    }
}