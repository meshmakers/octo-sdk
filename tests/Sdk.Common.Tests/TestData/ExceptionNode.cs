using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;

namespace Sdk.Common.Tests.TestData;

[NodeName("Exception", 1)]
internal class ExceptionNodeConfiguration : TargetPathNodeConfiguration;

[NodeConfiguration(typeof(ExceptionNodeConfiguration))]
internal class ExceptionNode(NodeDelegate next) : IPipelineNode
{
    /// <inheritdoc />
    public async Task ProcessObjectAsync(IDataContext dataContext)
    {
        throw new MyCustomException("Test exception");

#pragma warning disable CS0162 // Unreachable code detected
        await next(dataContext);
#pragma warning restore CS0162 // Unreachable code detected
    }
}