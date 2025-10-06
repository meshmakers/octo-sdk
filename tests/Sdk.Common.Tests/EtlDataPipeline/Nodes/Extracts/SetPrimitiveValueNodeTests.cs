#pragma warning disable CS8602 // Dereference of a possibly null reference.

using FakeItEasy;
using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Extracts;
using Meshmakers.Octo.Sdk.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sdk.Common.Tests.Fixtures;

namespace Sdk.Common.Tests.EtlDataPipeline.Nodes.Extracts;

public class SetPrimitiveValueNodeTests(NodeFixture fixture) : IClassFixture<NodeFixture>
{
    private (DataContext, INodeContext) PrepareTest(SetPrimitiveValueNodeConfiguration configuration)
    {
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = JObject.FromObject(new
            {
                existingValue = "original",
                existingNumber = 42,
                nested = new
                {
                    property = "value"
                }
            })
        };
        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("SetPrimitiveValue", 0, configuration, dataContext);
        return (dataContext, nodeContext);
    }

    [Fact]
    public async Task ProcessObjectAsync_SetString_OK()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.newStringValue",
            Value = "test string",
            ValueType = AttributeValueTypesDto.String,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("test string", dataContext.Current["newStringValue"]!.ToObject<string>());
    }

    [Fact]
    public async Task ProcessObjectAsync_SetInt_OK()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.newIntValue",
            Value = 123,
            ValueType = AttributeValueTypesDto.Int,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(123, dataContext.Current["newIntValue"]!.ToObject<int>());
    }

    [Fact]
    public async Task ProcessObjectAsync_SetIntFromString_OK()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.convertedIntValue",
            Value = "456",
            ValueType = AttributeValueTypesDto.Int,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(456, dataContext.Current["convertedIntValue"]!.ToObject<int>());
    }

    [Fact]
    public async Task ProcessObjectAsync_SetInt64_OK()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.newLongValue",
            Value = 9876543210L,
            ValueType = AttributeValueTypesDto.Int64,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(9876543210L, dataContext.Current["newLongValue"]!.ToObject<long>());
    }

    [Fact]
    public async Task ProcessObjectAsync_SetDouble_OK()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.newDoubleValue",
            Value = 3.14159,
            ValueType = AttributeValueTypesDto.Double,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(3.14159, dataContext.Current["newDoubleValue"]!.ToObject<double>(), 5);
    }

    [Fact]
    public async Task ProcessObjectAsync_SetDoubleFromString_InvariantCulture_OK()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.parsedDoubleValue",
            Value = "123.456789", // Using dot as decimal separator
            ValueType = AttributeValueTypesDto.Double,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(123.456789, dataContext.Current["parsedDoubleValue"]!.ToObject<double>(), 6);
    }

    [Fact]
    public async Task ProcessObjectAsync_SetBoolean_OK()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.newBooleanValue",
            Value = true,
            ValueType = AttributeValueTypesDto.Boolean,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.True(dataContext.Current["newBooleanValue"]!.ToObject<bool>());
    }

    [Fact]
    public async Task ProcessObjectAsync_SetBooleanFromString_OK()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.convertedBooleanValue",
            Value = "false",
            ValueType = AttributeValueTypesDto.Boolean,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.False(dataContext.Current["convertedBooleanValue"]!.ToObject<bool>());
    }

    [Fact]
    public async Task ProcessObjectAsync_SetDateTime_OK()
    {
        // Arrange
        var expectedDate = new DateTime(2023, 10, 15, 14, 30, 0, DateTimeKind.Utc);
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.newDateTimeValue",
            Value = expectedDate,
            ValueType = AttributeValueTypesDto.DateTime,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(expectedDate, dataContext.Current["newDateTimeValue"]!.ToObject<DateTime>());
    }

    [Fact]
    public async Task ProcessObjectAsync_SetDateTimeFromString_OK()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.parsedDateTimeValue",
            Value = "2023-12-25T00:00:00Z",
            ValueType = AttributeValueTypesDto.DateTime,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var result = dataContext.Current["parsedDateTimeValue"]!.ToObject<DateTime>();
        Assert.Equal(new DateTime(2023, 12, 25, 0, 0, 0), result.Date);
    }

    [Fact]
    public async Task ProcessObjectAsync_SetTimeSpan_OK()
    {
        // Arrange
        var expectedTimeSpan = TimeSpan.FromHours(2.5);
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.newTimeSpanValue",
            Value = expectedTimeSpan,
            ValueType = AttributeValueTypesDto.TimeSpan,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(expectedTimeSpan, dataContext.Current["newTimeSpanValue"]!.ToObject<TimeSpan>());
    }

    [Fact]
    public async Task ProcessObjectAsync_SetBinary_OK()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.newBinaryValue",
            Value = (byte)255,
            ValueType = AttributeValueTypesDto.Binary,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal((byte)255, dataContext.Current["newBinaryValue"]!.ToObject<byte>());
    }

    [Fact]
    public async Task ProcessObjectAsync_SetStringArray_OK()
    {
        // Arrange
        var expectedArray = new[] { "item1", "item2", "item3" };
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.newStringArrayValue",
            Value = expectedArray,
            ValueType = AttributeValueTypesDto.StringArray,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var result = dataContext.Current["newStringArrayValue"]!.ToObject<string[]>();
        Assert.Equal(expectedArray, result);
    }

    [Fact]
    public async Task ProcessObjectAsync_SetIntArray_OK()
    {
        // Arrange
        var expectedArray = new[] { 1, 2, 3, 4, 5 };
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.newIntArrayValue",
            Value = expectedArray,
            ValueType = AttributeValueTypesDto.IntArray,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var result = dataContext.Current["newIntArrayValue"]!.ToObject<int[]>();
        Assert.Equal(expectedArray, result);
    }

    [Fact]
    public async Task ProcessObjectAsync_SetNestedPath_OK()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.nested.newProperty",
            Value = "nested value",
            ValueType = AttributeValueTypesDto.String,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("nested value", dataContext.Current["nested"]!["newProperty"]!.ToObject<string>());
    }

    [Fact]
    public async Task ProcessObjectAsync_OverwriteExistingValue_OK()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.existingValue",
            Value = "overwritten value",
            ValueType = AttributeValueTypesDto.String,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("overwritten value", dataContext.Current["existingValue"]!.ToObject<string>());
    }

    [Fact]
    public async Task ProcessObjectAsync_NullValue_OK()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.nullValue",
            Value = null!,
            ValueType = AttributeValueTypesDto.String,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Null(dataContext.Current["nullValue"]!.ToObject<string>());
    }

    [Fact]
    public async Task ProcessObjectAsync_UnsupportedValueType_ThrowsException()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.testValue",
            Value = "test",
            ValueType = (AttributeValueTypesDto)999, // Invalid value type
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_InvalidConversion_ThrowsException()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.testValue",
            Value = "not a number",
            ValueType = AttributeValueTypesDto.Int,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_InvalidDateTimeFormat_ThrowsException()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.testValue",
            Value = "invalid date format",
            ValueType = AttributeValueTypesDto.DateTime,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_ZeroValues_OK()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.zeroValue",
            Value = 0,
            ValueType = AttributeValueTypesDto.Int,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(0, dataContext.Current["zeroValue"]!.ToObject<int>());
    }

    [Fact]
    public async Task ProcessObjectAsync_EmptyString_OK()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.emptyStringValue",
            Value = "",
            ValueType = AttributeValueTypesDto.String,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("", dataContext.Current["emptyStringValue"]!.ToObject<string>());
    }

    [Fact]
    public async Task ProcessObjectAsync_EmptyArray_OK()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.emptyArrayValue",
            Value = new string[0],
            ValueType = AttributeValueTypesDto.StringArray,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        var result = dataContext.Current["emptyArrayValue"]!.ToObject<string[]>();
        Assert.Empty(result!);
    }

    [Fact]
    public async Task ProcessObjectAsync_LargeNumbers_OK()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.largeNumberValue",
            Value = double.MaxValue,
            ValueType = AttributeValueTypesDto.Double,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(double.MaxValue, dataContext.Current["largeNumberValue"]!.ToObject<double>());
    }

    [Fact]
    public async Task ProcessObjectAsync_NegativeNumbers_OK()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.negativeValue",
            Value = -42,
            ValueType = AttributeValueTypesDto.Int,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(-42, dataContext.Current["negativeValue"]!.ToObject<int>());
    }

    [Fact]
    public async Task ProcessObjectAsync_SetStringFromValuePath_OK()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.copiedValue",
            ValuePath = "$.existingValue", // Copy from existing value
            ValueType = AttributeValueTypesDto.String,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("original", dataContext.Current["copiedValue"]!.ToObject<string>());
    }

    [Fact]
    public async Task ProcessObjectAsync_SetIntFromValuePath_OK()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.copiedNumber",
            ValuePath = "$.existingNumber", // Copy from existing number
            ValueType = AttributeValueTypesDto.Int,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(42, dataContext.Current["copiedNumber"]!.ToObject<int>());
    }

    [Fact]
    public async Task ProcessObjectAsync_SetFromNestedValuePath_OK()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.extractedProperty",
            ValuePath = "$.nested.property", // Copy from nested property
            ValueType = AttributeValueTypesDto.String,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("value", dataContext.Current["extractedProperty"]!.ToObject<string>());
    }

    [Fact]
    public async Task ProcessObjectAsync_ValuePathTakesPrecedenceOverValue_OK()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.resultValue",
            Value = "static value", // This should be ignored
            ValuePath = "$.existingValue", // This should take precedence
            ValueType = AttributeValueTypesDto.String,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("original", dataContext.Current["resultValue"]!.ToObject<string>()); // Should use ValuePath, not Value
    }

    [Fact]
    public async Task ProcessObjectAsync_ValuePathNotFound_ThrowsException()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.resultValue",
            ValuePath = "$.nonexistentPath",
            ValueType = AttributeValueTypesDto.String,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<PipelineExecutionException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_EmptyValuePath_UsesStaticValue_OK()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.resultValue",
            Value = "static value",
            ValuePath = "", // Empty string should use static Value
            ValueType = AttributeValueTypesDto.String,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("static value", dataContext.Current["resultValue"]!.ToObject<string>());
    }

    [Fact]
    public async Task ProcessObjectAsync_NullValuePath_UsesStaticValue_OK()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.resultValue",
            Value = "static value",
            ValuePath = null, // Null should use static Value
            ValueType = AttributeValueTypesDto.String,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal("static value", dataContext.Current["resultValue"]!.ToObject<string>());
    }

    [Fact]
    public async Task ProcessObjectAsync_ValuePathWithTypeConversion_OK()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.doubleFromInt",
            ValuePath = "$.existingNumber", // Get int value and convert to double
            ValueType = AttributeValueTypesDto.Double,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Equal(42.0, dataContext.Current["doubleFromInt"]!.ToObject<double>());
    }

    [Fact]
    public async Task ProcessObjectAsync_NullValueForString_OK()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.nullStringValue",
            Value = null,
            ValueType = AttributeValueTypesDto.String,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Null(dataContext.Current["nullStringValue"]!.ToObject<string>());
    }

    [Fact]
    public async Task ProcessObjectAsync_NullValueForInt_ThrowsException()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.nullIntValue",
            Value = null,
            ValueType = AttributeValueTypesDto.Int,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<PipelineExecutionException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_NullValueForDouble_ThrowsException()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.nullDoubleValue",
            Value = null,
            ValueType = AttributeValueTypesDto.Double,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<PipelineExecutionException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_NullValueForBoolean_ThrowsException()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.nullBooleanValue",
            Value = null,
            ValueType = AttributeValueTypesDto.Boolean,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<PipelineExecutionException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_NullValueForDateTime_ThrowsException()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.nullDateTimeValue",
            Value = null,
            ValueType = AttributeValueTypesDto.DateTime,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<PipelineExecutionException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_NullValueForStringArray_ThrowsException()
    {
        // Arrange
        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.nullArrayValue",
            Value = null,
            ValueType = AttributeValueTypesDto.StringArray,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var (dataContext, nodeContext) = PrepareTest(configuration);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<PipelineExecutionException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }

    [Fact]
    public async Task ProcessObjectAsync_NullValueFromValuePath_ForString_OK()
    {
        // Arrange - Create data context with a null value at a path
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = Newtonsoft.Json.Linq.JObject.FromObject(new
            {
                nullProperty = (string?)null,
                existingValue = "original"
            })
        };

        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.copiedNullValue",
            ValuePath = "$.nullProperty", // Copy null value from path
            ValueType = AttributeValueTypesDto.String,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("SetPrimitiveValue", 0, configuration, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act
        await testee.ProcessObjectAsync(dataContext, nodeContext);

        // Assert
        A.CallTo(() => fn.Invoke(dataContext, nodeContext)).MustHaveHappenedOnceExactly();
        Assert.Null(dataContext.Current["copiedNullValue"]!.ToObject<string>());
    }

    [Fact]
    public async Task ProcessObjectAsync_NullValueFromValuePath_ForInt_ThrowsException()
    {
        // Arrange - Create data context with a null value at a path
        var logger = A.Fake<IPipelineLogger>();
        var dataContext = new DataContext
        {
            Current = Newtonsoft.Json.Linq.JObject.FromObject(new
            {
                nullProperty = (int?)null,
                existingValue = "original"
            })
        };

        var configuration = new SetPrimitiveValueNodeConfiguration
        {
            TargetPath = "$.copiedNullValue",
            ValuePath = "$.nullProperty", // Try to copy null value for Int type
            ValueType = AttributeValueTypesDto.Int,
            DocumentMode = DocumentModes.Extend,
            TargetValueKind = ValueKinds.Simple,
            TargetValueWriteMode = TargetValueWriteModes.Overwrite
        };

        var rootNodeContext = NodeContext.CreateRootNodeContext(fixture.Services.BuildServiceProvider(), logger, dataContext);
        var nodeContext = rootNodeContext.RegisterChildNode("SetPrimitiveValue", 0, configuration, dataContext);
        var fn = A.Fake<NodeDelegate>();
        var testee = new SetPrimitiveValueNode(fn);

        // Act & Assert
        await Assert.ThrowsAsync<PipelineExecutionException>(() => testee.ProcessObjectAsync(dataContext, nodeContext));
    }
}