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

public class Base64EncodeNodeTests(NodeFixture fixture) : IClassFixture<NodeFixture>
{
    private (DataContext, INodeContext) PrepareTest(Base64EncodeNodeConfiguration configuration)
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                items = new[]
                {
                    new { text = "Hello World", id = 1 },
                    new { text = "Test String", id = 2 },
                    new { text = "Special chars: äöü €@#", id = 3 }
                },
                singleValue = "Plain text",
                nested = new
                {
                    secret = "my-secret-key"
                },
                nullValue = (string?)null,
                emptyValue = ""
            })
        };
        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Base64Encode", 0, configuration, dataContext);
        return (dataContext, nodeContext);
    }

    [Fact]
    public async Task ProcessObjectAsync_EncodeSimpleString_OK()
    {
        // Arrange
        var configuration = new Base64EncodeNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.singleValue",
            TargetPath = "$.encodedValue"
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new Base64EncodeNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("UGxhaW4gdGV4dA==", dataContext.Current["encodedValue"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_EncodeArrayItems_OK()
    {
        // Arrange
        var configuration = new Base64EncodeNodeConfiguration
        {
            Path = "$.items[*]",
            SourcePath = "$.text",
            TargetPath = "$.encoded"
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new Base64EncodeNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("SGVsbG8gV29ybGQ=", dataContext.Current["items"]![0]!["encoded"]!.ToString());
        Assert.Equal("VGVzdCBTdHJpbmc=", dataContext.Current["items"]![1]!["encoded"]!.ToString());
        Assert.Equal("U3BlY2lhbCBjaGFyczogw6TDtsO8IOKCrEAj", dataContext.Current["items"]![2]!["encoded"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_EncodeNestedValue_OK()
    {
        // Arrange
        var configuration = new Base64EncodeNodeConfiguration
        {
            Path = "$.nested",
            SourcePath = "$.secret",
            TargetPath = "$.encodedSecret"
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new Base64EncodeNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("bXktc2VjcmV0LWtleQ==", dataContext.Current["nested"]!["encodedSecret"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_EncodeNullValue_StaysNull()
    {
        // Arrange
        var configuration = new Base64EncodeNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.nullValue",
            TargetPath = "$.encodedNull"
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new Base64EncodeNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var encodedNull = dataContext.Current["encodedNull"];
        Assert.True(encodedNull == null || encodedNull.Type == Newtonsoft.Json.Linq.JTokenType.Null);
    }

    [Fact]
    public async Task ProcessObjectAsync_EncodeEmptyString_OK()
    {
        // Arrange
        var configuration = new Base64EncodeNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.emptyValue",
            TargetPath = "$.encodedEmpty"
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new Base64EncodeNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("", dataContext.Current["encodedEmpty"]!.ToString()); // Empty string encodes to empty Base64
    }

    [Fact]
    public async Task ProcessObjectAsync_NullDataContext_ThrowsException()
    {
        // Arrange
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext { Current = null };
        var configuration = new Base64EncodeNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.value",
            TargetPath = "$.encoded"
        };
        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Base64Encode", 0, configuration, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new Base64EncodeNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<PipelineExecutionException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_NoSourceData_Warning()
    {
        // Arrange
        var configuration = new Base64EncodeNodeConfiguration
        {
            Path = "$.nonexistent[*]",
            SourcePath = "$.value",
            TargetPath = "$.encoded"
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new Base64EncodeNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ProcessObjectAsync_SpecialCharacters_OK()
    {
        // Arrange
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                special = "!@#$%^&*()_+-=[]{}|;':\",./<>?"
            })
        };

        var configuration = new Base64EncodeNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.special",
            TargetPath = "$.encoded"
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Base64Encode", 0, configuration, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new Base64EncodeNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("IUAjJCVeJiooKV8rLT1bXXt9fDsnOiIsLi88Pj8=", dataContext.Current["encoded"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_Unicode_OK()
    {
        // Arrange
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                unicode = "Hello 世界 مرحبا мир 🌍"
            })
        };

        var configuration = new Base64EncodeNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.unicode",
            TargetPath = "$.encoded"
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Base64Encode", 0, configuration, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new Base64EncodeNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("SGVsbG8g5LiW55WMINmF2LHYrdio2Kcg0LzQuNGAIPCfjI0=", dataContext.Current["encoded"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_OverwriteExisting_OK()
    {
        // Arrange
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                value = "test",
                encoded = "old-value"
            })
        };

        var configuration = new Base64EncodeNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.value",
            TargetPath = "$.encoded"
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Base64Encode", 0, configuration, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new Base64EncodeNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("dGVzdA==", dataContext.Current["encoded"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_LongString_OK()
    {
        // Arrange
        var longString = new string('A', 1000); // 1000 A's
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                longValue = longString
            })
        };

        var configuration = new Base64EncodeNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.longValue",
            TargetPath = "$.encoded"
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Base64Encode", 0, configuration, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new Base64EncodeNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var encoded = dataContext.Current["encoded"]!.ToString();
        Assert.NotNull(encoded);
        Assert.True(encoded.Length > 0);
        // Verify it's valid Base64
        var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        Assert.Equal(longString, decoded);
    }
}