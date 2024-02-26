using Microsoft.Extensions.DependencyInjection;
using Sdk.Common.Tests.TestData;

namespace Sdk.Common.Tests.Fixtures;

public class DataPipelineFixture : ServiceCollectionFixture
{
    public DataPipelineFixture()
    {
        Services.AddDataPipeline().RegisterNode<TestDataExtractNode>();
    }
    
}