using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;

namespace Sdk.Common.Tests.TestData;

[NodeName("TestOutput", 1)]
internal class TestOutputNodeConfiguration : TargetPathNodeConfiguration
{
    public object? TargetValue { get; set; }
}

[NodeConfiguration(typeof(TestOutputNodeConfiguration))]
internal class TestOutputNode(NodeDelegate next) : IPipelineNode
{
    /// <inheritdoc />
    public async Task ProcessObjectAsync(IDataContext dataContext)
    {
        var c = dataContext.NodeContext.GetNodeConfiguration<TestOutputNodeConfiguration>();

        dataContext.SetValueByPath(c.TargetPath, c.TargetValueKind, c.TargetValueWriteMode, c.TargetValue);
          
        await next(dataContext);
    }
}