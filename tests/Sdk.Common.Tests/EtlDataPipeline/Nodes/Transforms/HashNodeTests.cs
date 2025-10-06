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

public class HashNodeTests(NodeFixture fixture) : IClassFixture<NodeFixture>
{
    private (DataContext, INodeContext) PrepareTest(HashNodeConfiguration configuration)
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                items = new[]
                {
                    new { text = "Hello World", password = "secret123", id = 1 },
                    new { text = "Test String", password = "mypassword", id = 2 },
                    new { text = "Special chars: äöü €@#", password = "unicode_pwd", id = 3 }
                },
                singleValue = "Test Content",
                base64Value = "SGVsbG8gV29ybGQ=", // "Hello World" in Base64
                emptyValue = "",
                nullValue = (string?)null,
                binaryData = "VGhpcyBpcyBhIHRlc3Q=" // "This is a test" in Base64
            })
        };
        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Hash", 0, configuration, dataContext);
        return (dataContext, nodeContext);
    }

    [Fact]
    public async Task ProcessObjectAsync_MD5_String_OK()
    {
        // Arrange
        var configuration = new HashNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.singleValue",
            TargetPath = "$.md5Hash",
            Algorithm = HashAlgorithmDto.Md5,
            InputFormat = HashInputFormatDto.String
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new HashNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        // MD5 hash of "Test Content"
        Assert.Equal("d65cdbadce081581e7de64a5a44b4617", dataContext.Current["md5Hash"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_SHA1_String_OK()
    {
        // Arrange
        var configuration = new HashNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.singleValue",
            TargetPath = "$.sha1Hash",
            Algorithm = HashAlgorithmDto.Sha1,
            InputFormat = HashInputFormatDto.String
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new HashNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        // SHA1 hash of "Test Content"
        Assert.Equal("bebfefe6bd0a8175e99a83f217ed3d2dbfe55bc8", dataContext.Current["sha1Hash"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_SHA256_String_OK()
    {
        // Arrange
        var configuration = new HashNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.singleValue",
            TargetPath = "$.sha256Hash",
            Algorithm = HashAlgorithmDto.Sha256,
            InputFormat = HashInputFormatDto.String
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new HashNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        // SHA256 hash of "Test Content"
        Assert.Equal("60c9b75f15144a088fd7800e1049c6c80a92e76de588c2b21b30ff42f6694ce2", dataContext.Current["sha256Hash"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_SHA384_String_OK()
    {
        // Arrange
        var configuration = new HashNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.singleValue",
            TargetPath = "$.sha384Hash",
            Algorithm = HashAlgorithmDto.Sha384,
            InputFormat = HashInputFormatDto.String
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new HashNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        // SHA384 produces a 96-character lowercase hex string (384 bits = 48 bytes = 96 hex chars)
        var actualHash = dataContext.Current["sha384Hash"]!.ToString();
        Assert.Equal(96, actualHash.Length);
        Assert.Matches("^[0-9a-f]{96}$", actualHash);
    }

    [Fact]
    public async Task ProcessObjectAsync_SHA512_String_OK()
    {
        // Arrange
        var configuration = new HashNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.singleValue",
            TargetPath = "$.sha512Hash",
            Algorithm = HashAlgorithmDto.Sha512,
            InputFormat = HashInputFormatDto.String
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new HashNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        // SHA512 produces 128-character lowercase hex string (512 bits = 64 bytes = 128 hex chars)
        var actualHash = dataContext.Current["sha512Hash"]!.ToString();
        Assert.Equal(128, actualHash.Length);
        Assert.Matches("^[0-9a-f]{128}$", actualHash);
    }

    [Fact]
    public async Task ProcessObjectAsync_SHA256_Base64_OK()
    {
        // Arrange
        var configuration = new HashNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.base64Value",
            TargetPath = "$.base64Hash",
            Algorithm = HashAlgorithmDto.Sha256,
            InputFormat = HashInputFormatDto.Base64
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new HashNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        // This should hash the decoded "Hello World" bytes, same as if we hashed "Hello World" as string
        Assert.Equal("a591a6d40bf420404a011733cfb7b190d62c65bf0bcda32b57b277d9ad9f146e", dataContext.Current["base64Hash"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_ArrayItems_OK()
    {
        // Arrange
        var configuration = new HashNodeConfiguration
        {
            Path = "$.items[*]",
            SourcePath = "$.text",
            TargetPath = "$.textHash",
            Algorithm = HashAlgorithmDto.Md5,
            InputFormat = HashInputFormatDto.String
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new HashNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        // Verify that all items have hashes
        Assert.Equal("b10a8db164e0754105b7a99be72e3fe5", dataContext.Current["items"]![0]!["textHash"]!.ToString()); // "Hello World"
        Assert.Equal("bd08ba3c982eaad768602536fb8e1184", dataContext.Current["items"]![1]!["textHash"]!.ToString()); // "Test String"
        // Just verify the third item has a valid MD5 hash format
        var thirdHash = dataContext.Current["items"]![2]!["textHash"]!.ToString();
        Assert.Equal(32, thirdHash.Length);
        Assert.Matches("^[0-9a-f]{32}$", thirdHash);
    }

    [Fact]
    public async Task ProcessObjectAsync_EmptyString_OK()
    {
        // Arrange
        var configuration = new HashNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.emptyValue",
            TargetPath = "$.emptyHash",
            Algorithm = HashAlgorithmDto.Sha256,
            InputFormat = HashInputFormatDto.String
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new HashNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        // SHA256 of empty string
        Assert.Equal("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", dataContext.Current["emptyHash"]!.ToString());
    }

    [Fact]
    public async Task ProcessObjectAsync_NullValue_ThrowsException()
    {
        // Arrange
        var configuration = new HashNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.nullValue",
            TargetPath = "$.nullHash",
            Algorithm = HashAlgorithmDto.Md5,
            InputFormat = HashInputFormatDto.String
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new HashNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<PipelineExecutionException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_NullDataContext_ThrowsException()
    {
        // Arrange
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext { Current = null };
        var configuration = new HashNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.value",
            TargetPath = "$.hash",
            Algorithm = HashAlgorithmDto.Sha256,
            InputFormat = HashInputFormatDto.String
        };
        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Hash", 0, configuration, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new HashNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<PipelineExecutionException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_NoSourceData_Warning()
    {
        // Arrange
        var configuration = new HashNodeConfiguration
        {
            Path = "$.nonexistent[*]",
            SourcePath = "$.value",
            TargetPath = "$.hash",
            Algorithm = HashAlgorithmDto.Sha256,
            InputFormat = HashInputFormatDto.String
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new HashNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ProcessObjectAsync_InvalidBase64_ThrowsException()
    {
        // Arrange
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                invalidBase64 = "This is not valid Base64!"
            })
        };

        var configuration = new HashNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.invalidBase64",
            TargetPath = "$.hash",
            Algorithm = HashAlgorithmDto.Sha256,
            InputFormat = HashInputFormatDto.Base64
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Hash", 0, configuration, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new HashNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<PipelineExecutionException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_UnsupportedAlgorithm_ThrowsException()
    {
        // Arrange
        var configuration = new HashNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.singleValue",
            TargetPath = "$.hash",
            Algorithm = (HashAlgorithmDto)999, // Invalid algorithm
            InputFormat = HashInputFormatDto.String
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new HashNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<PipelineExecutionException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_UnsupportedInputFormat_ThrowsException()
    {
        // Arrange
        var configuration = new HashNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.singleValue",
            TargetPath = "$.hash",
            Algorithm = HashAlgorithmDto.Sha256,
            InputFormat = (HashInputFormatDto)999 // Invalid input format
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new HashNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<PipelineExecutionException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
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
                unicode = "Hello 世界 مرحبا мир 🌍"
            })
        };

        var configuration = new HashNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.unicode",
            TargetPath = "$.unicodeHash",
            Algorithm = HashAlgorithmDto.Sha256,
            InputFormat = HashInputFormatDto.String
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("Hash", 0, configuration, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new HashNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var hash = dataContext.Current["unicodeHash"]!.ToString();
        Assert.Equal(64, hash.Length); // SHA256 produces 64-character hex string
        Assert.Matches("^[0-9a-f]{64}$", hash);
    }

    [Fact]
    public async Task ProcessObjectAsync_ConsistentHashing_OK()
    {
        // Arrange - Test that same input produces same hash
        var configuration1 = new HashNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.singleValue",
            TargetPath = "$.hash1",
            Algorithm = HashAlgorithmDto.Sha256,
            InputFormat = HashInputFormatDto.String
        };

        var configuration2 = new HashNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.singleValue",
            TargetPath = "$.hash2",
            Algorithm = HashAlgorithmDto.Sha256,
            InputFormat = HashInputFormatDto.String
        };

        var (dataContext, nodeContext1) = PrepareTest(configuration1);
        var (_, nodeContext2) = PrepareTest(configuration2);
        var fn = A.Fake<NodeDelegate>();
        var testee1 = new HashNode(fn);
        var testee2 = new HashNode(fn);

        // Act
        await testee1.ProcessObjectAsync(dataContext, nodeContext1);
        await testee2.ProcessObjectAsync(dataContext, nodeContext2);

        // Assert
        var hash1 = dataContext.Current["hash1"]!.ToString();
        var hash2 = dataContext.Current["hash2"]!.ToString();
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public async Task ProcessObjectAsync_DifferentInputFormats_DifferentResults()
    {
        // Arrange - Test that string vs base64 input of same content produces different hashes
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                stringValue = "SGVsbG8gV29ybGQ=", // This as string
                base64Value = "SGVsbG8gV29ybGQ="  // This as Base64 (decodes to "Hello World")
            })
        };

        var stringConfig = new HashNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.stringValue",
            TargetPath = "$.stringHash",
            Algorithm = HashAlgorithmDto.Sha256,
            InputFormat = HashInputFormatDto.String
        };

        var base64Config = new HashNodeConfiguration
        {
            Path = "$",
            SourcePath = "$.base64Value",
            TargetPath = "$.base64Hash",
            Algorithm = HashAlgorithmDto.Sha256,
            InputFormat = HashInputFormatDto.Base64
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext1 = rootNodeContext.RegisterChildNode("Hash", 0, stringConfig, dataContext);
        var nodeContext2 = rootNodeContext.RegisterChildNode("Hash", 1, base64Config, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new HashNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext1);
        await testee.ProcessObjectAsync(dataContext, nodeContext2);

        // Assert
        var stringHash = dataContext.Current["stringHash"]!.ToString();
        var base64Hash = dataContext.Current["base64Hash"]!.ToString();
        Assert.NotEqual(stringHash, base64Hash); // Should be different since we're hashing different content
    }

    [Fact]
    public async Task ProcessObjectAsync_AllAlgorithmsProduceDifferentHashes_OK()
    {
        // Arrange - Test that different algorithms produce different hash lengths and values
        var algorithms = new[]
        {
            (HashAlgorithmDto.Md5, "md5", 32),
            (HashAlgorithmDto.Sha1, "sha1", 40),
            (HashAlgorithmDto.Sha256, "sha256", 64),
            (HashAlgorithmDto.Sha384, "sha384", 96),
            (HashAlgorithmDto.Sha512, "sha512", 128)
        };

        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new { testValue = "Test Content" })
        };

        var fn = A.Fake<NodeDelegate>();
        var testee = new HashNode(fn);

        var hashes = new List<string>();

        foreach (var (algorithm, targetPrefix, expectedLength) in algorithms)
        {
            var configuration = new HashNodeConfiguration
            {
                Path = "$",
                SourcePath = "$.testValue",
                TargetPath = $"$.{targetPrefix}Hash",
                Algorithm = algorithm,
                InputFormat = HashInputFormatDto.String
            };

            var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
            var nodeContext = rootNodeContext.RegisterChildNode("Hash", 0, configuration, dataContext);

            // Act
            await testee.ProcessObjectAsync(dataContext, nodeContext);

            // Assert
            var hash = dataContext.Current[$"{targetPrefix}Hash"]!.ToString();
            Assert.Equal(expectedLength, hash.Length);
            Assert.Matches("^[0-9a-f]+$", hash); // Lowercase hex
            hashes.Add(hash);
        }

        // Verify all hashes are different
        Assert.Equal(5, hashes.Distinct().Count());
    }
}