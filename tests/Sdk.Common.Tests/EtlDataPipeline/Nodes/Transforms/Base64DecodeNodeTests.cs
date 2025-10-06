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

public class Base64DecodeNodeTests(NodeFixture fixture) : IClassFixture<NodeFixture>
{
    private (DataContext, INodeContext) PrepareTest(Base64DecodeNodeConfiguration configuration)
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                items = new[]
                {
                    new { encoded = "SGVsbG8gV29ybGQ=", id = 1 }, // "Hello World"
                    new { encoded = "VGVzdCBTdHJpbmc=", id = 2 }, // "Test String"
                    new { encoded = "U3BlY2lhbCBjaGFyczogw6TDtsO8IOKCrEAj", id = 3 } // "Special chars: äöü €@#"
                },
                singleEncoded = "UGxhaW4gdGV4dA==", // "Plain text"
                nested = new
                {
                    encodedSecret = "***REMOVED-DEMO-AB3837***" // "my-secret-key"
                },
                nullValue = (string?)null,
                emptyValue = "",
                invalidBase64 = "This is not valid Base64!"
            })
        };
        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Base64Decode", 0, configuration, dataContext);
        return (dataContext, nodeContext);
    }

    [Fact]
    public async Task ProcessObjectAsync_DecodeSimpleString_OK()
    {
        // Arrange
        var configuration = new Base64DecodeNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.singleEncoded",
            TargetPath = "$.decodedValue"
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new Base64DecodeNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("Plain text", dataContext.Current["decodedValue"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_DecodeArrayItems_OK()
    {
        // Arrange
        var configuration = new Base64DecodeNodeConfiguration
        {
            Path = "$.items[*]",
            SourcePath = "$.encoded",
            TargetPath = "$.text"
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new Base64DecodeNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("Hello World", dataContext.Current["items"]![0]!["text"]!.ToString());
        Assert.Equal("Test String", dataContext.Current["items"]![1]!["text"]!.ToString());
        Assert.Equal("Special chars: äöü €@#", dataContext.Current["items"]![2]!["text"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_DecodeNestedValue_OK()
    {
        // Arrange
        var configuration = new Base64DecodeNodeConfiguration
        {
            Path = "$.nested",
            SourcePath = "$.encodedSecret",
            TargetPath = "$.secret"
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new Base64DecodeNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("my-secret-key", dataContext.Current["nested"]!["secret"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_DecodeNullValue_StaysNull()
    {
        // Arrange
        var configuration = new Base64DecodeNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.nullValue",
            TargetPath = "$.decodedNull"
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new Base64DecodeNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.True(dataContext.Current["decodedNull"]!.Type == Newtonsoft.Json.Linq.JTokenType.Null);
    }

    [Fact]
    public async Task ProcessObjectAsync_DecodeEmptyString_OK()
    {
        // Arrange
        var configuration = new Base64DecodeNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.emptyValue",
            TargetPath = "$.decodedEmpty"
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new Base64DecodeNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("", dataContext.Current["decodedEmpty"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_InvalidBase64_ThrowsFormatException()
    {
        // Arrange
        var configuration = new Base64DecodeNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.invalidBase64",
            TargetPath = "$.decoded"
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new Base64DecodeNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<FormatException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_NullDataContext_ThrowsException()
    {
        // Arrange
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext { Current = null };
        var configuration = new Base64DecodeNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.value",
            TargetPath = "$.decoded"
        };
        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Base64Decode", 0, configuration, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new Base64DecodeNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<PipelineExecutionException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_NoSourceData_Warning()
    {
        // Arrange
        var configuration = new Base64DecodeNodeConfiguration
        {
            Path = "$.nonexistent[*]",
            SourcePath = "$.value",
            TargetPath = "$.decoded"
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new Base64DecodeNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ProcessObjectAsync_DecodeSpecialCharacters_OK()
    {
        // Arrange
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                encoded = "IUAjJCVeJiooKV8rLT1bXXt9fDsnOiIsLi88Pj8=" // "!@#$%^&*()_+-=[]{}|;':\",./<>?"
            })
        };

        var configuration = new Base64DecodeNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.encoded",
            TargetPath = "$.special"
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Base64Decode", 0, configuration, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new Base64DecodeNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("!@#$%^&*()_+-=[]{}|;':\",./<>?", dataContext.Current["special"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_DecodeUnicode_OK()
    {
        // Arrange
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                encoded = "SGVsbG8g5LiW55WMINmF2LHYrdio2Kcg0LzQuNGAIPCfjI0=" // "Hello 世界 مرحبا мир 🌍"
            })
        };

        var configuration = new Base64DecodeNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.encoded",
            TargetPath = "$.unicode"
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Base64Decode", 0, configuration, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new Base64DecodeNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("Hello 世界 مرحبا мир 🌍", dataContext.Current["unicode"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_RoundTrip_OK()
    {
        // Arrange - First encode a string
        var originalText = "This is a test string with special chars: äöü €@#";
        var encodedValue = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(originalText));

        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                encoded = encodedValue
            })
        };

        var configuration = new Base64DecodeNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.encoded",
            TargetPath = "$.decoded"
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Base64Decode", 0, configuration, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new Base64DecodeNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(originalText, dataContext.Current["decoded"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_Base64WithPadding_OK()
    {
        // Arrange
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                // Base64 with different padding scenarios
                single = "YQ==",    // "a" - needs == padding
                doublePad = "YWI=",    // "ab" - needs = padding
                triple = "YWJj"     // "abc" - no padding needed
            })
        };

        var configuration = new Base64DecodeNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.single",
            TargetPath = "$.decodedSingle"
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Base64Decode", 0, configuration, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new Base64DecodeNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("a", dataContext.Current["decodedSingle"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_InvalidBase64Padding_ThrowsException()
    {
        // Arrange
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                badPadding = "YQ=" // Should be "YQ==" for "a"
            })
        };

        var configuration = new Base64DecodeNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.badPadding",
            TargetPath = "$.decoded"
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Base64Decode", 0, configuration, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new Base64DecodeNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<FormatException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }
}