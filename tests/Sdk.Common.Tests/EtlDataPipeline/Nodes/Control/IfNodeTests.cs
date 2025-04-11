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

public class IfNodeTests(NodeFixture fixture) 
    : IClassFixture<NodeFixture>
{
    private (DataContext, INodeContext) PrepareTest(IfNodeConfiguration ifNodeConfiguration)
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                StringValue = "test",
                BoolValue = true,
                IntValue = 10,
                DoubleValue = 10.5,
                DateValue = DateTime.Now,
            })
        };
        var rootNodeContext = 
            NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("If", 0, ifNodeConfiguration, dataContext);
        return (dataContext, nodeContext);
    }

    [Fact]
    public async Task ProcessObjectAsync_Equals_Condition_True_OK()
    {
        // Arrange
        IfNodeConfiguration ifNodeConfiguration = new()
        {
            Path = "$.StringValue",
            Value = "test",
            ValueType = AttributeValueTypesDto.String,
            Operator = CompareOperator.Equal,
            Transformations = new List<NodeConfiguration>
            {
                new TestNodeConfiguration
                {
                    TargetPath = "$.Result"
                }
            }
        };

        var testCounter = A.Fake<ITestCounter>();
        fixture.Services.AddSingleton(testCounter);
        A.CallTo(() => testCounter.GetNext()).Returns(1);

        var (dataContext, nodeContext) = PrepareTest(ifNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new IfNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => testCounter.GetNext()).MustHaveHappenedOnceExactly();
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(1, dataContext.GetSimpleValueByPath<int>("$.Result"));
    }

    [Fact]
    public async Task ProcessObjectAsync_Equals_Condition_False_OK()
    {
        // Arrange
        IfNodeConfiguration ifNodeConfiguration = new()
        {
            Path = "$.StringValue",
            Value = "es", 
            ValueType = AttributeValueTypesDto.String,
            Operator = CompareOperator.Equal,
            Transformations = new List<NodeConfiguration>
            {
                new TestNodeConfiguration
                {
                    TargetPath = "$.Result"
                }
            }
        };

        var testCounter = A.Fake<ITestCounter>();
        fixture.Services.AddSingleton(testCounter);
        A.CallTo(() => testCounter.GetNext()).Returns(1);

        var (dataContext, nodeContext) = PrepareTest(ifNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new IfNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => testCounter.GetNext()).MustNotHaveHappened();
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ProcessObjectAsync_NotEquals_Condition_True_OK()
    {
        // Arrange
        IfNodeConfiguration ifNodeConfiguration = new()
        {
            Path = "$.StringValue",
            Value = "not_matching",
            ValueType = AttributeValueTypesDto.String,
            Operator = CompareOperator.NotEqual,
            Transformations = new List<NodeConfiguration>
            {
                new TestNodeConfiguration
                {
                    TargetPath = "$.Result"
                }
            }
        };

        var testCounter = A.Fake<ITestCounter>();
        fixture.Services.AddSingleton(testCounter);
        A.CallTo(() => testCounter.GetNext()).Returns(1);

        var (dataContext, nodeContext) = PrepareTest(ifNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new IfNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => testCounter.GetNext()).MustHaveHappenedOnceExactly();
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(1, dataContext.GetSimpleValueByPath<int>("$.Result"));
    }
    
    [Fact]
    public async Task ProcessObjectAsync_Contains_Condition_True_OK()
    {
        // Arrange
        IfNodeConfiguration ifNodeConfiguration = new()
        {
            Path = "$.StringValue",
            Value = "es", // test contains "es"
            ValueType = AttributeValueTypesDto.String,
            Operator = CompareOperator.Contains,
            Transformations = new List<NodeConfiguration>
            {
                new TestNodeConfiguration
                {
                    TargetPath = "$.Result"
                }
            }
        };

        var testCounter = A.Fake<ITestCounter>();
        fixture.Services.AddSingleton(testCounter);
        A.CallTo(() => testCounter.GetNext()).Returns(1);

        var (dataContext, nodeContext) = PrepareTest(ifNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new IfNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => testCounter.GetNext()).MustHaveHappenedOnceExactly();
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(1, dataContext.GetSimpleValueByPath<int>("$.Result")); 
    }
    
    [Fact]
    public async Task ProcessObjectAsync_ValuePath_Instead_Of_Value_OK()
    {
        // Arrange
        IfNodeConfiguration ifNodeConfiguration = new()
        {
            Path = "$.StringValue",
            ValuePath = "$.StringValue",
            ValueType = AttributeValueTypesDto.String,
            Operator = CompareOperator.Equal,
            Transformations = new List<NodeConfiguration>
            {
                new TestNodeConfiguration
                {
                    TargetPath = "$.Result"
                }
            }
        };

        var testCounter = A.Fake<ITestCounter>();
        fixture.Services.AddSingleton(testCounter);
        A.CallTo(() => testCounter.GetNext()).Returns(1);

        var (dataContext, nodeContext) = PrepareTest(ifNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new IfNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => testCounter.GetNext()).MustHaveHappenedOnceExactly();
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(1, dataContext.GetSimpleValueByPath<int>("$.Result"));
    }
}