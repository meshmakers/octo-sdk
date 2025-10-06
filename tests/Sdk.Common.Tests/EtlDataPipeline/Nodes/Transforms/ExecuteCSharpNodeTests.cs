using FakeItEasy;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;
using Meshmakers.Octo.Sdk.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Transforms;

public class ExecuteCSharpNodeTests(NodeFixture fixture) : IClassFixture<NodeFixture>
{
    private readonly IEtlContext _etlContext = new DefaultEtlContext(
        "test-tenant",
        OctoObjectId.Parse("507f1f77bcf86cd799439011"),
        Guid.NewGuid(),
        new RtEntityId("TestModel/TestType", OctoObjectId.GenerateNewId()),
        DateTime.UtcNow,
        null,
        new GlobalConfiguration(new List<ConfigurationDto>()),
        new Dictionary<string, object?>()
    );

    private (DataContext, INodeContext) PrepareTest(ExecuteCSharpNodeConfiguration configuration, JObject? testData = null)
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = testData ?? new JObject()
        };
        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("ExecuteCSharp", 0, configuration, dataContext);
        return (dataContext, nodeContext);
    }

    [Fact]
    public async Task ProcessObjectAsync_SimpleCalculation_ReturnsCorrectResult()
    {
        // Arrange
        var config = new ExecuteCSharpNodeConfiguration
        {
            Code = "2 + 2",
            ReturnType = AttributeValueTypesDto.Int,
            TargetPath = "$.result"
        };
        var (dataContext, nodeContext) = PrepareTest(config);

        var fn = A.Fake<NodeDelegate>();
        var node = new ExecuteCSharpNode(fn, _etlContext);

        // Act
        await node.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        var result = dataContext.Current?.SelectToken("$.result");
        Assert.Equal(4, result?.Value<int>());
    }

    [Fact]
    public async Task ProcessObjectAsync_IsPrimeFunction_ReturnsCorrectResult()
    {
        // Arrange
        var config = new ExecuteCSharpNodeConfiguration
        {
            Code = @"
                int n = number;
                if (n <= 1) return false;
                for (int i = 2; i * i <= n; i++)
                {
                    if (n % i == 0) return false;
                }
                return true;
            ",
            Arguments = new List<ScriptArgument>
            {
                new() { Name = "number", ValuePath = "$.input", DataType = AttributeValueTypesDto.Int }
            },
            ReturnType = AttributeValueTypesDto.Boolean,
            TargetPath = "$.isPrime"
        };
        var testData = JObject.FromObject(new { input = 17 });
        var (dataContext, nodeContext) = PrepareTest(config, testData);

        var fn = A.Fake<NodeDelegate>();
        var node = new ExecuteCSharpNode(fn, _etlContext);

        // Act
        await node.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        var result = dataContext.Current?.SelectToken("$.isPrime");
        Assert.True(result?.Value<bool>());
    }

    [Fact]
    public async Task ProcessObjectAsync_WithFixedValue_UsesFixedValue()
    {
        // Arrange
        var config = new ExecuteCSharpNodeConfiguration
        {
            Code = "prefix + suffix",
            Arguments = new List<ScriptArgument>
            {
                new() { Name = "prefix", Value = "Hello, ", DataType = AttributeValueTypesDto.String },
                new() { Name = "suffix", ValuePath = "$.name", DataType = AttributeValueTypesDto.String }
            },
            ReturnType = AttributeValueTypesDto.String,
            TargetPath = "$.greeting"
        };
        var testData = JObject.FromObject(new { name = "World" });
        var (dataContext, nodeContext) = PrepareTest(config, testData);

        var fn = A.Fake<NodeDelegate>();
        var node = new ExecuteCSharpNode(fn, _etlContext);

        // Act
        await node.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        var result = dataContext.Current?.SelectToken("$.greeting");
        Assert.Equal("Hello, World", result?.Value<string>());
    }

    [Fact]
    public async Task ProcessObjectAsync_WithMathFunctions_CalculatesCorrectly()
    {
        // Arrange
        var config = new ExecuteCSharpNodeConfiguration
        {
            Code = "Math.Sqrt(value) + Math.Pow(2, 3)",
            Arguments = new List<ScriptArgument>
            {
                new() { Name = "value", ValuePath = "$.number", DataType = AttributeValueTypesDto.Double }
            },
            ReturnType = AttributeValueTypesDto.Double,
            TargetPath = "$.result"
        };
        var testData = JObject.FromObject(new { number = 16.0 });
        var (dataContext, nodeContext) = PrepareTest(config, testData);

        var fn = A.Fake<NodeDelegate>();
        var node = new ExecuteCSharpNode(fn, _etlContext);

        // Act
        await node.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        var result = dataContext.Current?.SelectToken("$.result");
        Assert.Equal(12.0, result?.Value<double>()); // sqrt(16) + 2^3 = 4 + 8 = 12
    }

    [Fact]
    public async Task ProcessObjectAsync_WithNullValue_HandlesNull()
    {
        // Arrange
        var config = new ExecuteCSharpNodeConfiguration
        {
            Code = "value == null ? \"NULL\" : value.ToString()",
            Arguments = new List<ScriptArgument>
            {
                new() { Name = "value", ValuePath = "$.missing", DataType = AttributeValueTypesDto.String }
            },
            ReturnType = AttributeValueTypesDto.String,
            TargetPath = "$.result"
        };
        var (dataContext, nodeContext) = PrepareTest(config);

        var fn = A.Fake<NodeDelegate>();
        var node = new ExecuteCSharpNode(fn, _etlContext);

        // Act
        await node.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        var result = dataContext.Current?.SelectToken("$.result");
        Assert.Equal("NULL", result?.Value<string>());
    }

    [Fact]
    public async Task ProcessObjectAsync_WithMultipleArguments_CalculatesCorrectly()
    {
        // Arrange
        var config = new ExecuteCSharpNodeConfiguration
        {
            Code = "(a + b) * c",
            Arguments = new List<ScriptArgument>
            {
                new() { Name = "a", ValuePath = "$.x", DataType = AttributeValueTypesDto.Int },
                new() { Name = "b", ValuePath = "$.y", DataType = AttributeValueTypesDto.Int },
                new() { Name = "c", ValuePath = "$.z", DataType = AttributeValueTypesDto.Int }
            },
            ReturnType = AttributeValueTypesDto.Int,
            TargetPath = "$.result"
        };
        var testData = JObject.FromObject(new { x = 2, y = 3, z = 4 });
        var (dataContext, nodeContext) = PrepareTest(config, testData);

        var fn = A.Fake<NodeDelegate>();
        var node = new ExecuteCSharpNode(fn, _etlContext);

        // Act
        await node.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        var result = dataContext.Current?.SelectToken("$.result");
        Assert.Equal(20, result?.Value<int>()); // (2 + 3) * 4 = 20
    }

    [Fact]
    public async Task ProcessObjectAsync_WithCompilationError_ThrowsException()
    {
        // Arrange
        var config = new ExecuteCSharpNodeConfiguration
        {
            Code = "this is not valid C# code",
            ReturnType = AttributeValueTypesDto.String,
            TargetPath = "$.result"
        };
        var (dataContext, nodeContext) = PrepareTest(config);

        var fn = A.Fake<NodeDelegate>();
        var node = new ExecuteCSharpNode(fn, _etlContext);

        // Act & Assert
        await Assert.ThrowsAsync<PipelineExecutionException>(
            () => node.ProcessObjectAsync(dataContext, nodeContext)
        );
    }

    [Fact]
    public async Task ProcessObjectAsync_WithRuntimeException_ThrowsException()
    {
        // Arrange
        var config = new ExecuteCSharpNodeConfiguration
        {
            Code = "1 / zero",
            Arguments = new List<ScriptArgument>
            {
                new() { Name = "zero", Value = 0, DataType = AttributeValueTypesDto.Int }
            },
            ReturnType = AttributeValueTypesDto.Int,
            TargetPath = "$.result"
        };
        var (dataContext, nodeContext) = PrepareTest(config);

        var fn = A.Fake<NodeDelegate>();
        var node = new ExecuteCSharpNode(fn, _etlContext);

        // Act & Assert
        await Assert.ThrowsAsync<PipelineExecutionException>(
            () => node.ProcessObjectAsync(dataContext, nodeContext)
        );
    }

    [Fact]
    public async Task ProcessObjectAsync_WithTimeout_ThrowsTimeoutException()
    {
        // Arrange
        var config = new ExecuteCSharpNodeConfiguration
        {
            Code = "while(true) { }",
            ReturnType = AttributeValueTypesDto.String,
            TargetPath = "$.result",
            TimeoutMs = 100
        };
        var (dataContext, nodeContext) = PrepareTest(config);

        var fn = A.Fake<NodeDelegate>();
        var node = new ExecuteCSharpNode(fn, _etlContext);

        // Act & Assert
        await Assert.ThrowsAsync<PipelineExecutionException>(
            () => node.ProcessObjectAsync(dataContext, nodeContext)
        );
    }

    [Fact]
    public async Task ProcessObjectAsync_WithCustomUsings_ImportsCorrectly()
    {
        // Arrange
        var config = new ExecuteCSharpNodeConfiguration
        {
            Code = "string.Join(\", \", new[] { \"a\", \"b\", \"c\" }.Select(s => s.ToUpper()))",
            Usings = new List<string> { "System.Linq" },
            ReturnType = AttributeValueTypesDto.String,
            TargetPath = "$.result"
        };
        var (dataContext, nodeContext) = PrepareTest(config);

        var fn = A.Fake<NodeDelegate>();
        var node = new ExecuteCSharpNode(fn, _etlContext);

        // Act
        await node.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        var result = dataContext.Current?.SelectToken("$.result");
        Assert.Equal("A, B, C", result?.Value<string>());
    }

    [Fact]
    public async Task ProcessObjectAsync_ScriptIsCached_UsesCachedVersion()
    {
        // Arrange
        var config = new ExecuteCSharpNodeConfiguration
        {
            Code = "counter + 1",
            Arguments = new List<ScriptArgument>
            {
                new() { Name = "counter", ValuePath = "$.count", DataType = AttributeValueTypesDto.Int }
            },
            ReturnType = AttributeValueTypesDto.Int,
            TargetPath = "$.result"
        };

        var fn = A.Fake<NodeDelegate>();
        var node = new ExecuteCSharpNode(fn, _etlContext);

        // Act - First execution
        var testData1 = JObject.FromObject(new { count = 5 });
        var (dataContext1, nodeContext1) = PrepareTest(config, testData1);
        await node.ProcessObjectAsync(dataContext1, nodeContext1);
        var result1 = dataContext1.Current?.SelectToken("$.result")?.Value<int>();

        // Act - Second execution (should use cached script)
        var testData2 = JObject.FromObject(new { count = 10 });
        var (dataContext2, nodeContext2) = PrepareTest(config, testData2);
        await node.ProcessObjectAsync(dataContext2, nodeContext2);
        var result2 = dataContext2.Current?.SelectToken("$.result")?.Value<int>();

        // Assert
        Assert.Equal(6, result1);
        Assert.Equal(11, result2);
    }

    [Fact]
    public async Task ProcessObjectAsync_WithBooleanLogic_EvaluatesCorrectly()
    {
        // Arrange
        var config = new ExecuteCSharpNodeConfiguration
        {
            Code = "age >= 18 && hasLicense",
            Arguments = new List<ScriptArgument>
            {
                new() { Name = "age", ValuePath = "$.person.age", DataType = AttributeValueTypesDto.Int },
                new() { Name = "hasLicense", ValuePath = "$.person.license", DataType = AttributeValueTypesDto.Boolean }
            },
            ReturnType = AttributeValueTypesDto.Boolean,
            TargetPath = "$.canDrive"
        };
        var testData = JObject.FromObject(new { person = new { age = 20, license = true } });
        var (dataContext, nodeContext) = PrepareTest(config, testData);

        var fn = A.Fake<NodeDelegate>();
        var node = new ExecuteCSharpNode(fn, _etlContext);

        // Act
        await node.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        var result = dataContext.Current?.SelectToken("$.canDrive");
        Assert.True(result?.Value<bool>());
    }
}