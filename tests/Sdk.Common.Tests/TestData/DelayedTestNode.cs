using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;

namespace Sdk.Common.Tests.TestData;

[NodeName("DelayedTest", 1)]
internal record DelayedTestNodeConfiguration : TargetPathNodeConfiguration;

[NodeConfiguration(typeof(DelayedTestNodeConfiguration))]
internal class DelayedTestNode(NodeDelegate next, ConcurrencyTracker concurrencyTracker) : IPipelineNode
{
    public async Task ProcessObjectAsync(IDataContext dataContext, INodeContext nodeContext)
    {
        var c = nodeContext.GetNodeConfiguration<DelayedTestNodeConfiguration>();

        concurrencyTracker.Enter();
        try
        {
            await Task.Delay(50);
            dataContext.SetValueByPath(c.TargetPath, c.DocumentMode, c.TargetValueKind, c.TargetValueWriteMode, 1);
        }
        finally
        {
            concurrencyTracker.Exit();
        }

        await next(dataContext, nodeContext);
    }
}
