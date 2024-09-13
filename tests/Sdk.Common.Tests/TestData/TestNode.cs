using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;

namespace Sdk.Common.Tests.TestData;

[NodeName("Test", 1)]
internal class TestNodeConfiguration : NodeConfiguration
{
    public string? TargetPath { get; set; }
}

[NodeConfiguration(typeof(TestNodeConfiguration))]
internal class TestNode(NodeDelegate next, ITestCounter testCounter) : IPipelineNode
{
    /// <inheritdoc />
    public async Task ProcessObjectAsync(IDataContext dataContext)
    {
        var c = dataContext.GetNodeConfiguration<TestNodeConfiguration>();

        dataContext.SetCurrentValueByPath(c.TargetPath ?? "$", testCounter.GetNext());
          
        await next(dataContext);
    }
}