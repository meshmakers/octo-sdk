using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Newtonsoft.Json.Linq;

namespace Sdk.Common.Tests.TestData;

[NodeName("TestDataExtractNode", 1)]
public class TestDataExtractNodeConfiguration : NodeConfiguration
{
    public object? Data { get; set; }    
}

[NodeConfiguration(typeof(TestDataExtractNodeConfiguration))]
internal class TestDataExtractNode(NodeDelegate next) : IPipelineNode
{
    public async Task ProcessObjectAsync(IDataContext dataContext)
    {
        var c = dataContext.GetNodeConfiguration<TestDataExtractNodeConfiguration>();
        
        dataContext.Current = JObject.FromObject(c.Data ?? new JObject());

        await next(dataContext);
    }
}