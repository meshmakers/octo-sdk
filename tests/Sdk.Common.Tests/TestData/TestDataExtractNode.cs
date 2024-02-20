using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;

namespace Sdk.Common.Tests.DataPipeline;

public class TestDataExtractNodeConfiguration : ExtractNodeConfiguration
{
    public object? Data { get; set; }    
}

[Node("TestDataExtractNode", 1, typeof(TestDataExtractNodeConfiguration))]
internal class TestDataExtractNode : IExtractPipelineNode
{
    public Task ProcessObjectAsync(IExtractDataContext dataContext)
    {
        var c = dataContext.GetNodeConfiguration<TestDataExtractNodeConfiguration>();
        
        dataContext.Source = c.Data;
        
        return Task.CompletedTask;
    }
}