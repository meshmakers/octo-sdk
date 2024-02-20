using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Microsoft.Extensions.DependencyInjection;
using Sdk.Common.Tests.DataPipeline;

namespace Sdk.Common.Tests.Fixtures;

public class DataPipelineFixture : ServiceCollectionFixture
{
    public DataPipelineFixture()
    {
        Services.AddDataPipeline();
        Services.AddTransient<IExtractPipelineNode, TestDataExtractNode>();
    }
    
}