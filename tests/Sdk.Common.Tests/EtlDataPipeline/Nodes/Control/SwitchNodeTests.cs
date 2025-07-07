using FakeItEasy;
using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;
using Sdk.Common.Tests.TestData;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Control;

public class SwitchNodeTests(NodeFixture fixture) 
    : IClassFixture<NodeFixture>
{
    private (DataContext, INodeContext) PrepareTest(SwitchNodeConfiguration switchNodeConfiguration)
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                StringValue = "option1",
                IntValue = 42,
                BoolValue = true,
                DoubleValue = 3.14,
                EnumValue = 1
            })
        };
        var rootNodeContext = 
            NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Switch", 0, switchNodeConfiguration, dataContext);
        return (dataContext, nodeContext);
    }

    [Fact]
    public async Task ProcessObjectAsync_MatchingCase_ExecutesCorrectTransformations()
    {
        // Arrange
        var switchNodeConfiguration = new SwitchNodeConfiguration
        {
            Path = "$.StringValue",
            ValueType = AttributeValueTypesDto.String,
            Cases = new List<SwitchCase>
            {
                new()
                {
                    Value = "option1",
                    Transformations = new List<NodeConfiguration>
                    {
                        new TestNodeConfiguration { TargetPath = "$.Result1" }
                    }
                },
                new()
                {
                    Value = "option2",
                    Transformations = new List<NodeConfiguration>
                    {
                        new TestNodeConfiguration { TargetPath = "$.Result2" }
                    }
                }
            }
        };

        var testCounter = A.Fake<ITestCounter>();
        fixture.Services.AddSingleton(testCounter);
        A.CallTo(() => testCounter.GetNext()).Returns(100);

        var (dataContext, nodeContext) = PrepareTest(switchNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SwitchNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => testCounter.GetNext()).MustHaveHappenedOnceExactly();
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(100, dataContext.GetSimpleValueByPath<int>("$.Result1"));
        Assert.Null(dataContext.GetSimpleValueByPath<int?>("$.Result2"));
    }

    [Fact]
    public async Task ProcessObjectAsync_NoMatchingCase_ExecutesDefaultTransformations()
    {
        // Arrange
        var switchNodeConfiguration = new SwitchNodeConfiguration
        {
            Path = "$.StringValue",
            ValueType = AttributeValueTypesDto.String,
            Cases = new List<SwitchCase>
            {
                new()
                {
                    Value = "option2",
                    Transformations = new List<NodeConfiguration>
                    {
                        new TestNodeConfiguration { TargetPath = "$.CaseResult" }
                    }
                }
            },
            Default = new List<NodeConfiguration>
            {
                new TestNodeConfiguration { TargetPath = "$.DefaultResult" }
            }
        };

        var testCounter = A.Fake<ITestCounter>();
        fixture.Services.AddSingleton(testCounter);
        A.CallTo(() => testCounter.GetNext()).Returns(200);

        var (dataContext, nodeContext) = PrepareTest(switchNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SwitchNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => testCounter.GetNext()).MustHaveHappenedOnceExactly();
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(200, dataContext.GetSimpleValueByPath<int>("$.DefaultResult"));
        Assert.Null(dataContext.GetSimpleValueByPath<int?>("$.CaseResult"));
    }

    [Fact]
    public async Task ProcessObjectAsync_NoMatchingCaseAndNoDefault_DoesNothing()
    {
        // Arrange
        var switchNodeConfiguration = new SwitchNodeConfiguration
        {
            Path = "$.StringValue",
            ValueType = AttributeValueTypesDto.String,
            Cases = new List<SwitchCase>
            {
                new()
                {
                    Value = "option2",
                    Transformations = new List<NodeConfiguration>
                    {
                        new TestNodeConfiguration { TargetPath = "$.CaseResult" }
                    }
                }
            }
        };

        var testCounter = A.Fake<ITestCounter>();
        fixture.Services.AddSingleton(testCounter);

        var (dataContext, nodeContext) = PrepareTest(switchNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SwitchNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => testCounter.GetNext()).MustNotHaveHappened();
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Null(dataContext.GetSimpleValueByPath<int?>("$.CaseResult"));
    }

    [Fact]
    public async Task ProcessObjectAsync_IntegerCase_MatchesCorrectly()
    {
        // Arrange
        var switchNodeConfiguration = new SwitchNodeConfiguration
        {
            Path = "$.IntValue",
            ValueType = AttributeValueTypesDto.Int,
            Cases = new List<SwitchCase>
            {
                new()
                {
                    Value = 42,
                    Transformations = new List<NodeConfiguration>
                    {
                        new TestNodeConfiguration { TargetPath = "$.IntResult" }
                    }
                },
                new()
                {
                    Value = 99,
                    Transformations = new List<NodeConfiguration>
                    {
                        new TestNodeConfiguration { TargetPath = "$.OtherResult" }
                    }
                }
            }
        };

        var testCounter = A.Fake<ITestCounter>();
        fixture.Services.AddSingleton(testCounter);
        A.CallTo(() => testCounter.GetNext()).Returns(300);

        var (dataContext, nodeContext) = PrepareTest(switchNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SwitchNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => testCounter.GetNext()).MustHaveHappenedOnceExactly();
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(300, dataContext.GetSimpleValueByPath<int>("$.IntResult"));
        Assert.Null(dataContext.GetSimpleValueByPath<int?>("$.OtherResult"));
    }

    [Fact]
    public async Task ProcessObjectAsync_BooleanCase_MatchesCorrectly()
    {
        // Arrange
        var switchNodeConfiguration = new SwitchNodeConfiguration
        {
            Path = "$.BoolValue",
            ValueType = AttributeValueTypesDto.Boolean,
            Cases = new List<SwitchCase>
            {
                new()
                {
                    Value = true,
                    Transformations = new List<NodeConfiguration>
                    {
                        new TestNodeConfiguration { TargetPath = "$.TrueResult" }
                    }
                },
                new()
                {
                    Value = false,
                    Transformations = new List<NodeConfiguration>
                    {
                        new TestNodeConfiguration { TargetPath = "$.FalseResult" }
                    }
                }
            }
        };

        var testCounter = A.Fake<ITestCounter>();
        fixture.Services.AddSingleton(testCounter);
        A.CallTo(() => testCounter.GetNext()).Returns(400);

        var (dataContext, nodeContext) = PrepareTest(switchNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SwitchNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => testCounter.GetNext()).MustHaveHappenedOnceExactly();
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(400, dataContext.GetSimpleValueByPath<int>("$.TrueResult"));
        Assert.Null(dataContext.GetSimpleValueByPath<int?>("$.FalseResult"));
    }

    [Fact]
    public async Task ProcessObjectAsync_DoubleCase_MatchesCorrectly()
    {
        // Arrange
        var switchNodeConfiguration = new SwitchNodeConfiguration
        {
            Path = "$.DoubleValue",
            ValueType = AttributeValueTypesDto.Double,
            Cases = new List<SwitchCase>
            {
                new()
                {
                    Value = 3.14,
                    Transformations = new List<NodeConfiguration>
                    {
                        new TestNodeConfiguration { TargetPath = "$.PiResult" }
                    }
                }
            }
        };

        var testCounter = A.Fake<ITestCounter>();
        fixture.Services.AddSingleton(testCounter);
        A.CallTo(() => testCounter.GetNext()).Returns(500);

        var (dataContext, nodeContext) = PrepareTest(switchNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SwitchNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => testCounter.GetNext()).MustHaveHappenedOnceExactly();
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(500, dataContext.GetSimpleValueByPath<int>("$.PiResult"));
    }

    [Fact]
    public async Task ProcessObjectAsync_MultipleCasesWithSameValue_MatchesFirst()
    {
        // Arrange
        var switchNodeConfiguration = new SwitchNodeConfiguration
        {
            Path = "$.StringValue",
            ValueType = AttributeValueTypesDto.String,
            Cases = new List<SwitchCase>
            {
                new()
                {
                    Value = "option1",
                    Transformations = new List<NodeConfiguration>
                    {
                        new TestNodeConfiguration { TargetPath = "$.FirstMatch" }
                    }
                },
                new()
                {
                    Value = "option1", // Same value as first case
                    Transformations = new List<NodeConfiguration>
                    {
                        new TestNodeConfiguration { TargetPath = "$.SecondMatch" }
                    }
                }
            }
        };

        var testCounter = A.Fake<ITestCounter>();
        fixture.Services.AddSingleton(testCounter);
        A.CallTo(() => testCounter.GetNext()).Returns(600);

        var (dataContext, nodeContext) = PrepareTest(switchNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SwitchNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => testCounter.GetNext()).MustHaveHappenedOnceExactly();
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(600, dataContext.GetSimpleValueByPath<int>("$.FirstMatch"));
        Assert.Null(dataContext.GetSimpleValueByPath<int?>("$.SecondMatch"));
    }

    [Fact]
    public async Task ProcessObjectAsync_MultipleTransformationsInCase_ExecutesAll()
    {
        // Arrange
        var switchNodeConfiguration = new SwitchNodeConfiguration
        {
            Path = "$.StringValue",
            ValueType = AttributeValueTypesDto.String,
            Cases = new List<SwitchCase>
            {
                new()
                {
                    Value = "option1",
                    Transformations = new List<NodeConfiguration>
                    {
                        new TestNodeConfiguration { TargetPath = "$.Result1" },
                        new TestNodeConfiguration { TargetPath = "$.Result2" }
                    }
                }
            }
        };

        var testCounter = A.Fake<ITestCounter>();
        fixture.Services.AddSingleton(testCounter);
        var callCount = 0;
        A.CallTo(() => testCounter.GetNext()).ReturnsLazily(() => ++callCount * 100);

        var (dataContext, nodeContext) = PrepareTest(switchNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SwitchNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => testCounter.GetNext()).MustHaveHappenedTwiceExactly();
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(100, dataContext.GetSimpleValueByPath<int>("$.Result1"));
        Assert.Equal(200, dataContext.GetSimpleValueByPath<int>("$.Result2"));
    }
}
