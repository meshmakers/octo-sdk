using FakeItEasy;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;
using Sdk.Common.Tests.TestData;
using Sdk.Common.Tests.TestData.Dto;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Control;

public class ForEachNodeTests(NodeFixture fixture)
    : IClassFixture<NodeFixture>
{
    private DataContext PrepareTest(ForEachNodeConfiguration forEachNodeConfiguration)
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext(
            fixture.Services.BuildServiceProvider(), logger)
        {
            Current = JObject.FromObject(fixture.OrderDto)
        };
        dataContext.SetNodeConfiguration(forEachNodeConfiguration);
        return dataContext;
    }

    [Fact]
    public async Task ProcessObjectAsync_OK()
    {
        ForEachNodeConfiguration forEachNodeConfiguration = new()
        {
            Path = "$.Items",
            TargetPath = "$.Result",
            Transformations = new List<NodeConfiguration>
            {
                new TestNodeConfiguration()
            }
        };

        var testCounter = A.Fake<ITestCounter>();
        fixture.Services.AddSingleton(testCounter);
        A.CallTo(() => testCounter.GetNext()).Returns(0);

        var dataContext = PrepareTest(forEachNodeConfiguration);


        var fn = A.Fake<NodeDelegate>();
        var testee = new ForEachNode(fn);

        await testee.ProcessObjectAsync(dataContext);

        A.CallTo(() => testCounter.GetNext()).MustHaveHappened(3, Times.Exactly);
        A.CallTo(() => fn.Invoke(dataContext)).MustHaveHappenedOnceExactly();
        Assert.NotNull(dataContext.Current);
        Assert.Equal(3, dataContext.GetCurrentValuesByPath<int>("$.Result")?.Count());
    }

    [Fact]
    public async Task ProcessObjectAsync_SourceNull_OK()
    {
        ForEachNodeConfiguration forEachNodeConfiguration = new()
        {
            Path = "$.ItemsNotExisting",
            TargetPath = "$.Result",
            Transformations = new List<NodeConfiguration>
            {
                new TestNodeConfiguration()
            }
        };

        var testCounter = A.Fake<ITestCounter>();
        fixture.Services.AddSingleton(testCounter);
        A.CallTo(() => testCounter.GetNext()).Returns(0);
        
        var dataContext = PrepareTest(forEachNodeConfiguration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new ForEachNode(fn);

        await testee.ProcessObjectAsync(dataContext);

        A.CallTo(() => testCounter.GetNext()).MustNotHaveHappened();
        A.CallTo(() => fn.Invoke(dataContext)).MustHaveHappenedOnceExactly();
        Assert.NotNull(dataContext.Current);
        Assert.Equal(0, dataContext.GetCurrentValuesByPath<int>("$.Result")?.Count());
    }
}