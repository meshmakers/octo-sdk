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

public class TransformStringNodeTests(NodeFixture fixture) : IClassFixture<NodeFixture>
{
    private (DataContext, INodeContext) PrepareTest(TransformStringNodeConfiguration configuration)
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                items = new[]
                {
                    new { text = "  Hello World  ", name = "John Doe", email = "JOHN@EXAMPLE.COM" },
                    new { text = "Test String", name = "  jane smith  ", email = "jane@EXAMPLE.com" },
                    new { text = "Special chars: äöü €@#", name = "Bob Johnson", email = "BOB@test.COM" }
                },
                singleValue = "  Trim Me  ",
                longText = "This is a very long string for substring testing",
                emptyValue = "",
                nullValue = (string?)null
            })
        };
        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("TransformString", 0, configuration, dataContext);
        return (dataContext, nodeContext);
    }

    [Fact]
    public async Task ProcessObjectAsync_Trim_OK()
    {
        // Arrange
        var configuration = new TransformStringNodeConfiguration
        {
            Path = "$.items[*]",
            SourcePath = "$.text",
            TargetPath = "$.trimmed",
            Operation = StringOperationDto.Trim
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new TransformStringNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("Hello World", dataContext.Current["items"]![0]!["trimmed"]!.ToString());
        Assert.Equal("Test String", dataContext.Current["items"]![1]!["trimmed"]!.ToString());
        Assert.Equal("Special chars: äöü €@#", dataContext.Current["items"]![2]!["trimmed"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_TrimStart_OK()
    {
        // Arrange
        var configuration = new TransformStringNodeConfiguration
        {
            Path = "$.items[*]",
            SourcePath = "$.name",
            TargetPath = "$.trimmedStart",
            Operation = StringOperationDto.TrimStart
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new TransformStringNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("John Doe", dataContext.Current["items"]![0]!["trimmedStart"]!.ToString());
        Assert.Equal("jane smith  ", dataContext.Current["items"]![1]!["trimmedStart"]!.ToString());
        Assert.Equal("Bob Johnson", dataContext.Current["items"]![2]!["trimmedStart"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_TrimEnd_OK()
    {
        // Arrange
        var configuration = new TransformStringNodeConfiguration
        {
            Path = "$.items[*]",
            SourcePath = "$.name",
            TargetPath = "$.trimmedEnd",
            Operation = StringOperationDto.TrimEnd
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new TransformStringNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("John Doe", dataContext.Current["items"]![0]!["trimmedEnd"]!.ToString());
        Assert.Equal("  jane smith", dataContext.Current["items"]![1]!["trimmedEnd"]!.ToString());
        Assert.Equal("Bob Johnson", dataContext.Current["items"]![2]!["trimmedEnd"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_ToUpper_OK()
    {
        // Arrange
        var configuration = new TransformStringNodeConfiguration
        {
            Path = "$.items[*]",
            SourcePath = "$.name",
            TargetPath = "$.upperName",
            Operation = StringOperationDto.ToUpper
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new TransformStringNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("JOHN DOE", dataContext.Current["items"]![0]!["upperName"]!.ToString());
        Assert.Equal("  JANE SMITH  ", dataContext.Current["items"]![1]!["upperName"]!.ToString());
        Assert.Equal("BOB JOHNSON", dataContext.Current["items"]![2]!["upperName"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_ToLower_OK()
    {
        // Arrange
        var configuration = new TransformStringNodeConfiguration
        {
            Path = "$.items[*]",
            SourcePath = "$.email",
            TargetPath = "$.lowerEmail",
            Operation = StringOperationDto.ToLower
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new TransformStringNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("john@example.com", dataContext.Current["items"]![0]!["lowerEmail"]!.ToString());
        Assert.Equal("jane@example.com", dataContext.Current["items"]![1]!["lowerEmail"]!.ToString());
        Assert.Equal("bob@test.com", dataContext.Current["items"]![2]!["lowerEmail"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_SubstringFromStart_OK()
    {
        // Arrange
        var configuration = new TransformStringNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.longText",
            TargetPath = "$.prefix",
            Operation = StringOperationDto.SubstringFromStart,
            Length = 4
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new TransformStringNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("This", dataContext.Current["prefix"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_SubstringFromEnd_OK()
    {
        // Arrange
        var configuration = new TransformStringNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.longText",
            TargetPath = "$.suffix",
            Operation = StringOperationDto.SubstringFromEnd,
            Length = 7
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new TransformStringNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("testing", dataContext.Current["suffix"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_Substring_WithLength_OK()
    {
        // Arrange
        var configuration = new TransformStringNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.longText",
            TargetPath = "$.middle",
            Operation = StringOperationDto.Substring,
            StartIndex = 5,
            Length = 4
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new TransformStringNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("is a", dataContext.Current["middle"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_Substring_WithoutLength_OK()
    {
        // Arrange
        var configuration = new TransformStringNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.longText",
            TargetPath = "$.fromIndex",
            Operation = StringOperationDto.Substring,
            StartIndex = 40
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new TransformStringNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(" testing", dataContext.Current["fromIndex"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_SingleValue_OK()
    {
        // Arrange
        var configuration = new TransformStringNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.singleValue",
            TargetPath = "$.trimmedSingle",
            Operation = StringOperationDto.Trim
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new TransformStringNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("Trim Me", dataContext.Current["trimmedSingle"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_EmptyString_OK()
    {
        // Arrange
        var configuration = new TransformStringNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.emptyValue",
            TargetPath = "$.processedEmpty",
            Operation = StringOperationDto.ToUpper
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new TransformStringNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("", dataContext.Current["processedEmpty"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_NullValue_StaysNull()
    {
        // Arrange
        var configuration = new TransformStringNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.nullValue",
            TargetPath = "$.processedNull",
            Operation = StringOperationDto.Trim
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new TransformStringNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.True(dataContext.Current["processedNull"]!.Type == Newtonsoft.Json.Linq.JTokenType.Null);
    }

    [Fact]
    public async Task ProcessObjectAsync_NullDataContext_ThrowsException()
    {
        // Arrange
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext { Current = null };
        var configuration = new TransformStringNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.value",
            TargetPath = "$.processed",
            Operation = StringOperationDto.Trim
        };
        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("TransformString", 0, configuration, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new TransformStringNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<PipelineExecutionException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_NoSourceData_Warning()
    {
        // Arrange
        var configuration = new TransformStringNodeConfiguration
        {
            Path = "$.nonexistent[*]",
            SourcePath = "$.value",
            TargetPath = "$.processed",
            Operation = StringOperationDto.Trim
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new TransformStringNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ProcessObjectAsync_SubstringFromStart_ExceedsLength_OK()
    {
        // Arrange
        var configuration = new TransformStringNodeConfiguration
        {
            Path = "$.items[0]",
            SourcePath = "$.name",
            TargetPath = "$.prefix",
            Operation = StringOperationDto.SubstringFromStart,
            Length = 100 // Much longer than actual string
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new TransformStringNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("John Doe", dataContext.Current["items"]![0]!["prefix"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_SubstringFromEnd_ExceedsLength_OK()
    {
        // Arrange
        var configuration = new TransformStringNodeConfiguration
        {
            Path = "$.items[0]",
            SourcePath = "$.name",
            TargetPath = "$.suffix",
            Operation = StringOperationDto.SubstringFromEnd,
            Length = 100 // Much longer than actual string
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new TransformStringNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("John Doe", dataContext.Current["items"]![0]!["suffix"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_Substring_OutOfBounds_ReturnsEmpty()
    {
        // Arrange
        var configuration = new TransformStringNodeConfiguration
        {
            Path = "$.items[0]",
            SourcePath = "$.name",
            TargetPath = "$.outOfBounds",
            Operation = StringOperationDto.Substring,
            StartIndex = 100, // Beyond string length
            Length = 5
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new TransformStringNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("", dataContext.Current["items"]![0]!["outOfBounds"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_Substring_NegativeIndex_ReturnsEmpty()
    {
        // Arrange
        var configuration = new TransformStringNodeConfiguration
        {
            Path = "$.items[0]",
            SourcePath = "$.name",
            TargetPath = "$.negative",
            Operation = StringOperationDto.Substring,
            StartIndex = -5,
            Length = 3
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new TransformStringNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("", dataContext.Current["items"]![0]!["negative"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_SubstringFromStart_MissingLength_ThrowsException()
    {
        // Arrange
        var configuration = new TransformStringNodeConfiguration
        {
            Path = "$.items[0]",
            SourcePath = "$.name",
            TargetPath = "$.prefix",
            Operation = StringOperationDto.SubstringFromStart
            // Length not specified
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new TransformStringNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<PipelineExecutionException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_SubstringFromEnd_MissingLength_ThrowsException()
    {
        // Arrange
        var configuration = new TransformStringNodeConfiguration
        {
            Path = "$.items[0]",
            SourcePath = "$.name",
            TargetPath = "$.suffix",
            Operation = StringOperationDto.SubstringFromEnd
            // Length not specified
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new TransformStringNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<PipelineExecutionException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_UnsupportedOperation_ThrowsException()
    {
        // Arrange
        var configuration = new TransformStringNodeConfiguration
        {
            Path = "$.items[0]",
            SourcePath = "$.name",
            TargetPath = "$.result",
            Operation = (StringOperationDto)999 // Invalid operation
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new TransformStringNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_SubstringFromStart_ZeroLength_ReturnsEmpty()
    {
        // Arrange
        var configuration = new TransformStringNodeConfiguration
        {
            Path = "$.items[0]",
            SourcePath = "$.name",
            TargetPath = "$.empty",
            Operation = StringOperationDto.SubstringFromStart,
            Length = 0
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new TransformStringNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("", dataContext.Current["items"]![0]!["empty"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_CombinedOperations_OK()
    {
        // Arrange - Test chaining operations by applying trim then uppercase
        var trimConfiguration = new TransformStringNodeConfiguration
        {
            Path = "$.items[1]",
            SourcePath = "$.name",
            TargetPath = "$.trimmedName",
            Operation = StringOperationDto.Trim
        };

        var upperConfiguration = new TransformStringNodeConfiguration
        {
            Path = "$.items[1]",
            SourcePath = "$.trimmedName",
            TargetPath = "$.finalName",
            Operation = StringOperationDto.ToUpper
        };

        var (dataContext, nodeContext1) = PrepareTest(trimConfiguration);
        var (_, nodeContext2) = PrepareTest(upperConfiguration);
        var fn = A.Fake<NodeDelegate>();
        var trimNode = new TransformStringNode(fn);
        var upperNode = new TransformStringNode(fn);

        // Act
        await trimNode.ProcessObjectAsync(dataContext, nodeContext1);
        await upperNode.ProcessObjectAsync(dataContext, nodeContext2);

        // Assert
        Assert.Equal("jane smith", dataContext.Current["items"]![1]!["trimmedName"]!.ToString());
        Assert.Equal("JANE SMITH", dataContext.Current["items"]![1]!["finalName"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_UnicodeCharacters_OK()
    {
        // Arrange
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                unicode = new
                {
                    text = "  Hello 世界 مرحبا мир 🌍  "
                }
            })
        };

        var configuration = new TransformStringNodeConfiguration
        {
            Path = "$.unicode",
            SourcePath = "$.text",
            TargetPath = "$.processed",
            Operation = StringOperationDto.Trim
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("TransformString", 0, configuration, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new TransformStringNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("Hello 世界 مرحبا мир 🌍", dataContext.Current["unicode"]!["processed"]!.ToString());
    }
}