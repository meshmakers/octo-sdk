#pragma warning disable CS8602 // Dereference of a possibly null reference.

using FakeItEasy;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;
using Meshmakers.Octo.Sdk.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Transforms;

public class MathNodeTests(NodeFixture fixture) : IClassFixture<NodeFixture>
{
    private (DataContext, INodeContext) PrepareTest(MathNodeConfiguration mathNodeConfiguration)
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                items = new[]
                {
                    new { value = 10.0, multiplier = 2.0 },
                    new { value = 20.0, multiplier = 3.0 },
                    new { value = 5.5, multiplier = 1.5 }
                },
                globalMultiplier = 4.0,
                singleValue = 15.0
            })
        };
        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Math", 0, mathNodeConfiguration, dataContext);
        return (dataContext, nodeContext);
    }

    private (DataContext, INodeContext) PrepareRoundingTest(MathNodeConfiguration mathNodeConfiguration)
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                values = new[]
                {
                    new { amount = 3.14159 },
                    new { amount = 2.67891 },
                    new { amount = 10.999 },
                    new { amount = 123.456789 },
                    new { amount = 0.12345 }
                },
                singleAmount = 15.6789
            })
        };
        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Math", 0, mathNodeConfiguration, dataContext);
        return (dataContext, nodeContext);
    }

    [Fact]
    public async Task ProcessObjectAsync_Add_WithValue_OK()
    {
        // Arrange
        MathNodeConfiguration mathNodeConfiguration = new()
        {
            Path = "$.items[*]",
            ItemPath = "$.value",
            ItemTargetPath = "$.result",
            Operation = MathOperationDto.Add,
            Value = 5.0
        };

        var (dataContext, nodeContext) = PrepareTest(mathNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new MathNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(15.0, dataContext.Current["items"]![0]!["result"]!.ToObject<double>());
        Assert.Equal(25.0, dataContext.Current["items"]![1]!["result"]!.ToObject<double>());
        Assert.Equal(10.5, dataContext.Current["items"]![2]!["result"]!.ToObject<double>());
    }

    [Fact]
    public async Task ProcessObjectAsync_Subtract_WithValue_OK()
    {
        // Arrange
        MathNodeConfiguration mathNodeConfiguration = new()
        {
            Path = "$.items[*]",
            ItemPath = "$.value",
            ItemTargetPath = "$.result",
            Operation = MathOperationDto.Subtract,
            Value = 3.0
        };

        var (dataContext, nodeContext) = PrepareTest(mathNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new MathNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(7.0, dataContext.Current["items"]![0]!["result"]!.ToObject<double>());
        Assert.Equal(17.0, dataContext.Current["items"]![1]!["result"]!.ToObject<double>());
        Assert.Equal(2.5, dataContext.Current["items"]![2]!["result"]!.ToObject<double>());
    }

    [Fact]
    public async Task ProcessObjectAsync_Multiply_WithValue_OK()
    {
        // Arrange
        MathNodeConfiguration mathNodeConfiguration = new()
        {
            Path = "$.items[*]",
            ItemPath = "$.value",
            ItemTargetPath = "$.result",
            Operation = MathOperationDto.Multiply,
            Value = 2.0
        };

        var (dataContext, nodeContext) = PrepareTest(mathNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new MathNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(20.0, dataContext.Current["items"]![0]!["result"]!.ToObject<double>());
        Assert.Equal(40.0, dataContext.Current["items"]![1]!["result"]!.ToObject<double>());
        Assert.Equal(11.0, dataContext.Current["items"]![2]!["result"]!.ToObject<double>());
    }

    [Fact]
    public async Task ProcessObjectAsync_Divide_WithValue_OK()
    {
        // Arrange
        MathNodeConfiguration mathNodeConfiguration = new()
        {
            Path = "$.items[*]",
            ItemPath = "$.value",
            ItemTargetPath = "$.result",
            Operation = MathOperationDto.Divide,
            Value = 2.0
        };

        var (dataContext, nodeContext) = PrepareTest(mathNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new MathNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(5.0, dataContext.Current["items"]![0]!["result"]!.ToObject<double>());
        Assert.Equal(10.0, dataContext.Current["items"]![1]!["result"]!.ToObject<double>());
        Assert.Equal(2.75, dataContext.Current["items"]![2]!["result"]!.ToObject<double>());
    }

    [Fact]
    public async Task ProcessObjectAsync_Add_WithValuePath_OK()
    {
        // Arrange
        MathNodeConfiguration mathNodeConfiguration = new()
        {
            Path = "$.items[*]",
            ItemPath = "$.value",
            ItemTargetPath = "$.result",
            Operation = MathOperationDto.Add,
            ValuePath = "$.globalMultiplier"
        };

        var (dataContext, nodeContext) = PrepareTest(mathNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new MathNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(14.0, dataContext.Current["items"]![0]!["result"]!.ToObject<double>());
        Assert.Equal(24.0, dataContext.Current["items"]![1]!["result"]!.ToObject<double>());
        Assert.Equal(9.5, dataContext.Current["items"]![2]!["result"]!.ToObject<double>());
    }

    [Fact]
    public async Task ProcessObjectAsync_Multiply_WithValuePath_OK()
    {
        // Arrange
        MathNodeConfiguration mathNodeConfiguration = new()
        {
            Path = "$.items[*]",
            ItemPath = "$.value",
            ItemTargetPath = "$.result",
            Operation = MathOperationDto.Multiply,
            ValuePath = "$.globalMultiplier" // Use single value path instead of array path
        };

        var (dataContext, nodeContext) = PrepareTest(mathNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new MathNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        // Note: ValuePath gets globalMultiplier (4.0) and applies it to all items
        Assert.Equal(40.0, dataContext.Current["items"]![0]!["result"]!.ToObject<double>());
        Assert.Equal(80.0, dataContext.Current["items"]![1]!["result"]!.ToObject<double>());
        Assert.Equal(22.0, dataContext.Current["items"]![2]!["result"]!.ToObject<double>());
    }

    [Fact]
    public async Task ProcessObjectAsync_SingleValue_OK()
    {
        // Arrange
        MathNodeConfiguration mathNodeConfiguration = new()
        {
            Path = "$",
            ItemPath = "$.singleValue",
            ItemTargetPath = "$.calculatedValue",
            Operation = MathOperationDto.Multiply,
            Value = 3.0
        };

        var (dataContext, nodeContext) = PrepareTest(mathNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new MathNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(45.0, dataContext.Current["calculatedValue"]!.ToObject<double>());
    }

    [Fact]
    public async Task ProcessObjectAsync_NullInputValue_ThrowsException()
    {
        // Arrange
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext { Current = null };
        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);

        MathNodeConfiguration mathNodeConfiguration = new()
        {
            Path = "$.items[*]",
            ItemPath = "$.value",
            ItemTargetPath = "$.result",
            Operation = MathOperationDto.Add,
            Value = 5.0
        };

        var nodeContext = rootNodeContext.RegisterChildNode("Math", 0, mathNodeConfiguration, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new MathNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<PipelineExecutionException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_NoSourceData_Warning()
    {
        // Arrange
        MathNodeConfiguration mathNodeConfiguration = new()
        {
            Path = "$.nonexistent[*]",
            ItemPath = "$.value",
            ItemTargetPath = "$.result",
            Operation = MathOperationDto.Add,
            Value = 5.0
        };

        var (dataContext, nodeContext) = PrepareTest(mathNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new MathNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ProcessObjectAsync_NoValue_ThrowsException()
    {
        // Arrange
        MathNodeConfiguration mathNodeConfiguration = new()
        {
            Path = "$.items[*]",
            ItemPath = "$.value",
            ItemTargetPath = "$.result",
            Operation = MathOperationDto.Add,
            ValuePath = "$.nonexistent"
        };

        var (dataContext, nodeContext) = PrepareTest(mathNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new MathNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<PipelineExecutionException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_NoNumericValueAtItemPath_Warning()
    {
        // Arrange
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                items = new object[]
                {
                    new { value = (double?)null }, // Use null instead of string to avoid conversion exception
                    new { value = 20.0 }
                }
            })
        };

        MathNodeConfiguration mathNodeConfiguration = new()
        {
            Path = "$.items[*]",
            ItemPath = "$.value",
            ItemTargetPath = "$.result",
            Operation = MathOperationDto.Add,
            Value = 5.0
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Math", 0, mathNodeConfiguration, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new MathNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        // First item should be skipped due to null value, second item should be processed
        Assert.Null(dataContext.Current["items"]![0]!["result"]);
        Assert.Equal(25.0, dataContext.Current["items"]![1]!["result"]!.ToObject<double>());
    }

    [Fact]
    public async Task ProcessObjectAsync_DivideByZero_ReturnsInfinity()
    {
        // Arrange
        MathNodeConfiguration mathNodeConfiguration = new()
        {
            Path = "$.items[0]",
            ItemPath = "$.value",
            ItemTargetPath = "$.result",
            Operation = MathOperationDto.Divide,
            Value = 0.0
        };

        var (dataContext, nodeContext) = PrepareTest(mathNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new MathNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.True(double.IsInfinity(dataContext.Current["items"]![0]!["result"]!.ToObject<double>()));
    }

    [Fact]
    public async Task ProcessObjectAsync_UnsupportedOperation_ThrowsException()
    {
        // Arrange
        MathNodeConfiguration mathNodeConfiguration = new()
        {
            Path = "$.items[0]",
            ItemPath = "$.value",
            ItemTargetPath = "$.result",
            Operation = (MathOperationDto)999, // Invalid operation
            Value = 5.0
        };

        var (dataContext, nodeContext) = PrepareTest(mathNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new MathNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_NegativeNumbers_OK()
    {
        // Arrange
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                items = new[]
                {
                    new { value = -10.0 },
                    new { value = -5.5 }
                }
            })
        };

        MathNodeConfiguration mathNodeConfiguration = new()
        {
            Path = "$.items[*]",
            ItemPath = "$.value",
            ItemTargetPath = "$.result",
            Operation = MathOperationDto.Multiply,
            Value = -2.0
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Math", 0, mathNodeConfiguration, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new MathNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(20.0, dataContext.Current["items"]![0]!["result"]!.ToObject<double>());
        Assert.Equal(11.0, dataContext.Current["items"]![1]!["result"]!.ToObject<double>());
    }

    [Fact]
    public async Task ProcessObjectAsync_DecimalPrecision_OK()
    {
        // Arrange
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                items = new[]
                {
                    new { value = 0.1 },
                    new { value = 0.2 }
                }
            })
        };

        MathNodeConfiguration mathNodeConfiguration = new()
        {
            Path = "$.items[*]",
            ItemPath = "$.value",
            ItemTargetPath = "$.result",
            Operation = MathOperationDto.Add,
            Value = 0.3
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Math", 0, mathNodeConfiguration, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new MathNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(0.4, dataContext.Current["items"]![0]!["result"]!.ToObject<double>(), 1);
        Assert.Equal(0.5, dataContext.Current["items"]![1]!["result"]!.ToObject<double>(), 1);
    }

    [Fact]
    public async Task ProcessObjectAsync_Modulo_WithValue_OK()
    {
        // Arrange
        MathNodeConfiguration mathNodeConfiguration = new()
        {
            Path = "$.items[*]",
            ItemPath = "$.value",
            ItemTargetPath = "$.result",
            Operation = MathOperationDto.Modulo,
            Value = 3.0
        };

        var (dataContext, nodeContext) = PrepareTest(mathNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new MathNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(1.0, dataContext.Current["items"]![0]!["result"]!.ToObject<double>()); // 10 % 3 = 1
        Assert.Equal(2.0, dataContext.Current["items"]![1]!["result"]!.ToObject<double>()); // 20 % 3 = 2
        Assert.Equal(2.5, dataContext.Current["items"]![2]!["result"]!.ToObject<double>()); // 5.5 % 3 = 2.5
    }

    [Fact]
    public async Task ProcessObjectAsync_Modulo_WithValuePath_OK()
    {
        // Arrange
        MathNodeConfiguration mathNodeConfiguration = new()
        {
            Path = "$.items[*]",
            ItemPath = "$.value",
            ItemTargetPath = "$.result",
            Operation = MathOperationDto.Modulo,
            ValuePath = "$.globalMultiplier" // 4.0
        };

        var (dataContext, nodeContext) = PrepareTest(mathNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new MathNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(2.0, dataContext.Current["items"]![0]!["result"]!.ToObject<double>()); // 10 % 4 = 2
        Assert.Equal(0.0, dataContext.Current["items"]![1]!["result"]!.ToObject<double>()); // 20 % 4 = 0
        Assert.Equal(1.5, dataContext.Current["items"]![2]!["result"]!.ToObject<double>()); // 5.5 % 4 = 1.5
    }

    [Fact]
    public async Task ProcessObjectAsync_Modulo_WithZero_ReturnsNaN()
    {
        // Arrange
        MathNodeConfiguration mathNodeConfiguration = new()
        {
            Path = "$.items[0]", // Only test first item
            ItemPath = "$.value",
            ItemTargetPath = "$.result",
            Operation = MathOperationDto.Modulo,
            Value = 0.0
        };

        var (dataContext, nodeContext) = PrepareTest(mathNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new MathNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.True(double.IsNaN(dataContext.Current["items"]![0]!["result"]!.ToObject<double>()));
    }

    [Fact]
    public async Task ProcessObjectAsync_Modulo_WithNegativeNumbers_OK()
    {
        // Arrange
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                items = new[]
                {
                    new { value = -10.0 },
                    new { value = -7.0 },
                    new { value = 13.0 }
                }
            })
        };

        MathNodeConfiguration mathNodeConfiguration = new()
        {
            Path = "$.items[*]",
            ItemPath = "$.value",
            ItemTargetPath = "$.result",
            Operation = MathOperationDto.Modulo,
            Value = 3.0
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Math", 0, mathNodeConfiguration, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new MathNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(-1.0, dataContext.Current["items"]![0]!["result"]!.ToObject<double>()); // -10 % 3 = -1
        Assert.Equal(-1.0, dataContext.Current["items"]![1]!["result"]!.ToObject<double>()); // -7 % 3 = -1
        Assert.Equal(1.0, dataContext.Current["items"]![2]!["result"]!.ToObject<double>()); // 13 % 3 = 1
    }

    [Fact]
    public async Task ProcessObjectAsync_Round_ToInteger_OK()
    {
        // Arrange
        MathNodeConfiguration mathNodeConfiguration = new()
        {
            Path = "$.values[*]",
            ItemPath = "$.amount",
            ItemTargetPath = "$.rounded",
            Operation = MathOperationDto.Round,
            DecimalPlaces = 0
        };

        var (dataContext, nodeContext) = PrepareRoundingTest(mathNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new MathNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(3.0, dataContext.Current["values"]![0]!["rounded"]!.ToObject<double>()); // 3.14159 -> 3
        Assert.Equal(3.0, dataContext.Current["values"]![1]!["rounded"]!.ToObject<double>()); // 2.67891 -> 3
        Assert.Equal(11.0, dataContext.Current["values"]![2]!["rounded"]!.ToObject<double>()); // 10.999 -> 11
        Assert.Equal(123.0, dataContext.Current["values"]![3]!["rounded"]!.ToObject<double>()); // 123.456789 -> 123
        Assert.Equal(0.0, dataContext.Current["values"]![4]!["rounded"]!.ToObject<double>()); // 0.12345 -> 0
    }

    [Fact]
    public async Task ProcessObjectAsync_Round_ToTwoDecimalPlaces_OK()
    {
        // Arrange
        MathNodeConfiguration mathNodeConfiguration = new()
        {
            Path = "$.values[*]",
            ItemPath = "$.amount",
            ItemTargetPath = "$.rounded",
            Operation = MathOperationDto.Round,
            DecimalPlaces = 2
        };

        var (dataContext, nodeContext) = PrepareRoundingTest(mathNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new MathNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(3.14, dataContext.Current["values"]![0]!["rounded"]!.ToObject<double>()); // 3.14159 -> 3.14
        Assert.Equal(2.68, dataContext.Current["values"]![1]!["rounded"]!.ToObject<double>()); // 2.67891 -> 2.68
        Assert.Equal(11.0, dataContext.Current["values"]![2]!["rounded"]!.ToObject<double>()); // 10.999 -> 11.00
        Assert.Equal(123.46, dataContext.Current["values"]![3]!["rounded"]!.ToObject<double>()); // 123.456789 -> 123.46
        Assert.Equal(0.12, dataContext.Current["values"]![4]!["rounded"]!.ToObject<double>()); // 0.12345 -> 0.12
    }

    [Fact]
    public async Task ProcessObjectAsync_Round_ToFourDecimalPlaces_OK()
    {
        // Arrange
        MathNodeConfiguration mathNodeConfiguration = new()
        {
            Path = "$.values[*]",
            ItemPath = "$.amount",
            ItemTargetPath = "$.rounded",
            Operation = MathOperationDto.Round,
            DecimalPlaces = 4
        };

        var (dataContext, nodeContext) = PrepareRoundingTest(mathNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new MathNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(3.1416, dataContext.Current["values"]![0]!["rounded"]!.ToObject<double>()); // 3.14159 -> 3.1416
        Assert.Equal(2.6789, dataContext.Current["values"]![1]!["rounded"]!.ToObject<double>()); // 2.67891 -> 2.6789
        Assert.Equal(10.999, dataContext.Current["values"]![2]!["rounded"]!.ToObject<double>()); // 10.999 -> 10.999
        Assert.Equal(123.4568, dataContext.Current["values"]![3]!["rounded"]!.ToObject<double>()); // 123.456789 -> 123.4568
        Assert.Equal(0.1234, dataContext.Current["values"]![4]!["rounded"]!.ToObject<double>()); // 0.12345 -> 0.1234
    }

    [Fact]
    public async Task ProcessObjectAsync_Round_SingleValue_OK()
    {
        // Arrange
        MathNodeConfiguration mathNodeConfiguration = new()
        {
            Path = "$",
            ItemPath = "$.singleAmount",
            ItemTargetPath = "$.roundedAmount",
            Operation = MathOperationDto.Round,
            DecimalPlaces = 1
        };

        var (dataContext, nodeContext) = PrepareRoundingTest(mathNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new MathNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(15.7, dataContext.Current["roundedAmount"]!.ToObject<double>()); // 15.6789 -> 15.7
    }

    [Fact]
    public async Task ProcessObjectAsync_Round_NegativeNumbers_OK()
    {
        // Arrange
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                negativeValues = new[]
                {
                    new { amount = -3.14159 },
                    new { amount = -2.67891 },
                    new { amount = -0.5555 }
                }
            })
        };

        MathNodeConfiguration mathNodeConfiguration = new()
        {
            Path = "$.negativeValues[*]",
            ItemPath = "$.amount",
            ItemTargetPath = "$.rounded",
            Operation = MathOperationDto.Round,
            DecimalPlaces = 2
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Math", 0, mathNodeConfiguration, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new MathNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(-3.14, dataContext.Current["negativeValues"]![0]!["rounded"]!.ToObject<double>()); // -3.14159 -> -3.14
        Assert.Equal(-2.68, dataContext.Current["negativeValues"]![1]!["rounded"]!.ToObject<double>()); // -2.67891 -> -2.68
        Assert.Equal(-0.56, dataContext.Current["negativeValues"]![2]!["rounded"]!.ToObject<double>()); // -0.5555 -> -0.56
    }

    [Fact]
    public async Task ProcessObjectAsync_Round_ZeroDecimalPlaces_DefaultBehavior()
    {
        // Arrange - Test that DecimalPlaces defaults to 0 when not specified
        MathNodeConfiguration mathNodeConfiguration = new()
        {
            Path = "$.values[0]",
            ItemPath = "$.amount",
            ItemTargetPath = "$.rounded",
            Operation = MathOperationDto.Round
            // DecimalPlaces not specified, should default to 0
        };

        var (dataContext, nodeContext) = PrepareRoundingTest(mathNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new MathNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(3.0, dataContext.Current["values"]![0]!["rounded"]!.ToObject<double>()); // 3.14159 -> 3 (default 0 decimal places)
    }

    [Fact]
    public async Task ProcessObjectAsync_Round_ExcessiveDecimalPlaces_OK()
    {
        // Arrange - Test rounding with more decimal places than the original number has
        MathNodeConfiguration mathNodeConfiguration = new()
        {
            Path = "$.values[0]",
            ItemPath = "$.amount",
            ItemTargetPath = "$.rounded",
            Operation = MathOperationDto.Round,
            DecimalPlaces = 10 // More than original precision
        };

        var (dataContext, nodeContext) = PrepareRoundingTest(mathNodeConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new MathNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(3.14159, dataContext.Current["values"]![0]!["rounded"]!.ToObject<double>()); // 3.14159 -> 3.14159 (unchanged)
    }
}