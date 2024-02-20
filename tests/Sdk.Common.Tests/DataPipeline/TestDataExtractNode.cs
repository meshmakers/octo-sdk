using Meshmakers.Octo.Sdk.Common.DataPipeline;
using Meshmakers.Octo.Sdk.Common.DataPipeline.Configuration;

namespace Sdk.Common.Tests.DataPipeline;

public class TestDataExtractConfigurationNode : ExtractConfigurationNode
{
    public object? Data { get; set; }    
}

[Node("TestDataExtractNode", 1, typeof(TestDataExtractConfigurationNode))]
internal class TestDataExtractNode : IExtractPipelineNode
{
    public Task ProcessObjectAsync(IExtractDataContext dataContext)
    {
        var c = dataContext.GetNodeConfiguration<TestDataExtractConfigurationNode>();
        
        dataContext.Source = c.Data;
        
        return Task.CompletedTask;
    }
}