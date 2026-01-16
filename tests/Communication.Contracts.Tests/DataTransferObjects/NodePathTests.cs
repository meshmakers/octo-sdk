using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

namespace Communication.Contracts.Tests.DataTransferObjects;

public class NodePathTests
{
    [Fact]
    public void Constructor_WithPath_SetsPath()
    {
        // Arrange & Act
        var nodePath = new NodePath("root/child");

        // Assert
        Assert.Equal("root/child", nodePath.ToString());
    }

    [Fact]
    public void Constructor_WithNull_SetsEmptyPath()
    {
        // Arrange & Act
        var nodePath = new NodePath(null);

        // Assert
        Assert.Equal(string.Empty, nodePath.ToString());
    }

    [Fact]
    public void Constructor_Default_SetsEmptyPath()
    {
        // Arrange & Act
        var nodePath = new NodePath();

        // Assert
        Assert.Equal(string.Empty, nodePath.ToString());
    }

    [Fact]
    public void ImplicitConversion_FromString_CreatesNodePath()
    {
        // Arrange & Act
        NodePath nodePath = "test/path";

        // Assert
        Assert.Equal("test/path", nodePath.ToString());
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsPath()
    {
        // Arrange
        var nodePath = new NodePath("test/path");

        // Act
        string result = nodePath;

        // Assert
        Assert.Equal("test/path", result);
    }

    [Fact]
    public void Append_ToEmptyPath_ReturnsQualifiedName()
    {
        // Arrange
        var nodePath = new NodePath();

        // Act
        var result = nodePath.Append("child");

        // Assert
        Assert.Equal("child", result.ToString());
    }

    [Fact]
    public void Append_ToExistingPath_AppendsWithDelimiter()
    {
        // Arrange
        var nodePath = new NodePath("root");

        // Act
        var result = nodePath.Append("child");

        // Assert
        Assert.Equal("root/child", result.ToString());
    }

    [Fact]
    public void Append_MultipleAppends_BuildsCorrectPath()
    {
        // Arrange
        var nodePath = new NodePath("root");

        // Act
        var result = nodePath
            .Append("level1")
            .Append("level2")
            .Append("leaf");

        // Assert
        Assert.Equal("root/level1/level2/leaf", result.ToString());
    }

    [Fact]
    public void Equals_SamePath_ReturnsTrue()
    {
        // Arrange
        var nodePath1 = new NodePath("root/child");
        var nodePath2 = new NodePath("root/child");

        // Act & Assert
        Assert.True(nodePath1.Equals(nodePath2));
        Assert.True(nodePath1.Equals((object)nodePath2));
    }

    [Fact]
    public void Equals_DifferentPath_ReturnsFalse()
    {
        // Arrange
        var nodePath1 = new NodePath("root/child1");
        var nodePath2 = new NodePath("root/child2");

        // Act & Assert
        Assert.False(nodePath1.Equals(nodePath2));
    }

    [Fact]
    public void Equals_NullObject_ReturnsFalse()
    {
        // Arrange
        var nodePath = new NodePath("root");

        // Act & Assert
        Assert.False(nodePath.Equals((object?)null));
    }

    [Fact]
    public void Equals_StringWithSameValue_ReturnsTrue_DueToImplicitConversion()
    {
        // Arrange
        var nodePath = new NodePath("root");

        // Act & Assert
        // Note: Due to implicit conversion from string to NodePath, this returns true
        Assert.True(nodePath.Equals("root"));
    }

    [Fact]
    public void Equals_NonConvertibleType_ReturnsFalse()
    {
        // Arrange
        var nodePath = new NodePath("root");

        // Act & Assert
        Assert.False(nodePath.Equals(123));
    }

    [Fact]
    public void GetHashCode_SamePath_ReturnsSameHashCode()
    {
        // Arrange
        var nodePath1 = new NodePath("root/child");
        var nodePath2 = new NodePath("root/child");

        // Act & Assert
        Assert.Equal(nodePath1.GetHashCode(), nodePath2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentPath_ReturnsDifferentHashCode()
    {
        // Arrange
        var nodePath1 = new NodePath("root/child1");
        var nodePath2 = new NodePath("root/child2");

        // Act & Assert
        Assert.NotEqual(nodePath1.GetHashCode(), nodePath2.GetHashCode());
    }

    [Fact]
    public void CompareTo_SamePath_ReturnsZero()
    {
        // Arrange
        var nodePath1 = new NodePath("root/child");
        var nodePath2 = new NodePath("root/child");

        // Act & Assert
        Assert.Equal(0, nodePath1.CompareTo(nodePath2));
    }

    [Fact]
    public void CompareTo_LessThan_ReturnsNegative()
    {
        // Arrange
        var nodePath1 = new NodePath("aaa");
        var nodePath2 = new NodePath("bbb");

        // Act & Assert
        Assert.True(nodePath1.CompareTo(nodePath2) < 0);
    }

    [Fact]
    public void CompareTo_GreaterThan_ReturnsPositive()
    {
        // Arrange
        var nodePath1 = new NodePath("bbb");
        var nodePath2 = new NodePath("aaa");

        // Act & Assert
        Assert.True(nodePath1.CompareTo(nodePath2) > 0);
    }

    [Fact]
    public void GetTypeCode_ReturnsObject()
    {
        // Arrange
        var nodePath = new NodePath("root");

        // Act
        var typeCode = nodePath.GetTypeCode();

        // Assert
        Assert.Equal(TypeCode.Object, typeCode);
    }

    [Fact]
    public void ToStringWithProvider_ReturnsPath()
    {
        // Arrange
        var nodePath = new NodePath("root/child");

        // Act
        var result = nodePath.ToString(null);

        // Assert
        Assert.Equal("root/child", result);
    }

    [Fact]
    public void ToType_ToStringType_ReturnsPathAsString()
    {
        // Arrange
        var nodePath = new NodePath("root/child");

        // Act
        var result = nodePath.ToType(typeof(string), null);

        // Assert
        Assert.Equal("root/child", result);
    }

    [Fact]
    public void ToType_ToNodePathType_ReturnsSelf()
    {
        // Arrange
        var nodePath = new NodePath("root/child");

        // Act
        var result = nodePath.ToType(typeof(NodePath), null);

        // Assert
        Assert.Equal(nodePath, result);
    }

    [Fact]
    public void ToType_ToObjectType_ReturnsSelf()
    {
        // Arrange
        var nodePath = new NodePath("root/child");

        // Act
        var result = nodePath.ToType(typeof(object), null);

        // Assert
        Assert.Equal(nodePath, result);
    }

    [Theory]
    [InlineData(typeof(int))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(Guid))]
    public void ToType_UnsupportedType_ThrowsInvalidCastException(Type targetType)
    {
        // Arrange
        var nodePath = new NodePath("root");

        // Act & Assert
        Assert.Throws<InvalidCastException>(() => nodePath.ToType(targetType, null));
    }

    [Fact]
    public void ToBoolean_ThrowsInvalidCastException()
    {
        // Arrange
        var nodePath = new NodePath("root");

        // Act & Assert
        Assert.Throws<InvalidCastException>(() => nodePath.ToBoolean(null));
    }

    [Fact]
    public void ToInt32_ThrowsInvalidCastException()
    {
        // Arrange
        var nodePath = new NodePath("123");

        // Act & Assert
        Assert.Throws<InvalidCastException>(() => nodePath.ToInt32(null));
    }

    [Fact]
    public void ToDouble_ThrowsInvalidCastException()
    {
        // Arrange
        var nodePath = new NodePath("1.5");

        // Act & Assert
        Assert.Throws<InvalidCastException>(() => nodePath.ToDouble(null));
    }
}
