using FakeItEasy;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;
using Sdk.Common.Tests.TestData.Dto;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Transforms;

public class ProjectNodeTests(NodeFixture fixture) : IClassFixture<NodeFixture>
{
    private (DataContext, INodeContext) PrepareTest(ProjectNodeConfiguration projectNodeConfiguration)
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(Generator.GenerateColumnData())
        };
        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Project", 0, projectNodeConfiguration, dataContext);
        return (dataContext, nodeContext);
    }
    
    
    [Fact]
    public async Task ProcessObjectAsync_ExcludeByField_OK()
    {
        ProjectNodeConfiguration projectNodeConfiguration = new()
        {
            Fields = new List<FieldConfiguration>
            {
                new()
                {
                    Path = "$.data.timestamp"
                },
                new()
                {
                    Path = "$.data.batteryPower"
                }
            }
        };

        var (dataContext, nodeContext) = PrepareTest(projectNodeConfiguration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new ProjectNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.NotNull(dataContext.Current);
        Assert.Equal(5, dataContext.Current["data"]?.Count());
        Assert.NotNull(dataContext.Current["data"]?["productionPower"]);
        Assert.Null(dataContext.Current["data"]?["timestamp"]);
    }

    [Fact]
    public async Task ProcessObjectAsync_UsePathAndExcludeByField_OK()
    {
        ProjectNodeConfiguration projectNodeConfiguration = new()
        {
            Path = "$.data",
            Fields = new List<FieldConfiguration>
            {
                new()
                {
                    Path = "$.timestamp"
                },
                new()
                {
                    Path = "$.batteryPower"
                }
            }
        };

        var (dataContext, nodeContext) = PrepareTest(projectNodeConfiguration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new ProjectNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.NotNull(dataContext.Current);
        Assert.Equal(5, dataContext.Current["data"]?.Count());
        Assert.NotNull(dataContext.Current["data"]?["productionPower"]);
        Assert.Null(dataContext.Current["data"]?["timestamp"]);
    }
    
    [Fact]
    public async Task ProcessObjectAsync_ClearAndWhitelist_OK()
    {
        ProjectNodeConfiguration projectNodeConfiguration = new()
        {
            Clear = true,
            Fields = new List<FieldConfiguration>
            {
                new()
                {
                    Path = "$.data.timestamp",
                    Inclusion = true
                },
                new()
                {
                    Path = "$.data.batteryPower",
                    Inclusion = true
                }
            }
        };

        var (dataContext, nodeContext) = PrepareTest(projectNodeConfiguration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new ProjectNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.NotNull(dataContext.Current);
        Assert.Equal(2, dataContext.Current["data"]?.Count());
        Assert.Null(dataContext.Current["data"]?["productionPower"]);
        Assert.NotNull(dataContext.Current["data"]?["timestamp"]);
        Assert.NotNull(dataContext.Current["data"]?["batteryPower"]);
    }
    
    [Fact]
    public async Task ProcessObjectAsync_UsePathClearAndWhitelist_OK()
    {
        ProjectNodeConfiguration projectNodeConfiguration = new()
        {
            Path = "$.data",
            Clear = true,
            Fields = new List<FieldConfiguration>
            {
                new()
                {
                    Path = "$.timestamp",
                    Inclusion = true
                },
                new()
                {
                    Path = "$.batteryPower",
                    Inclusion = true
                }
            }
        };

        var (dataContext, nodeContext)  = PrepareTest(projectNodeConfiguration);

        var fn = A.Fake<NodeDelegate>();
        var testee = new ProjectNode(fn);

        await testee.ProcessObjectAsync(dataContext, nodeContext);

        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.NotNull(dataContext.Current);
        Assert.Equal(2, dataContext.Current["data"]?.Count());
        Assert.Null(dataContext.Current["data"]?["productionPower"]);
        Assert.NotNull(dataContext.Current["data"]?["timestamp"]);
        Assert.NotNull(dataContext.Current["data"]?["batteryPower"]);
    }
}