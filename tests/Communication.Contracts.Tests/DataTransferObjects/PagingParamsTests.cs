using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

namespace Communication.Contracts.Tests.DataTransferObjects;

public class PagingParamsTests
{
    [Fact]
    public void Constructor_Default_SetsDefaultValues()
    {
        // Act
        var pagingParams = new PagingParams();

        // Assert
        Assert.Equal(0, pagingParams.Skip);
        Assert.Equal(100, pagingParams.Take);
        Assert.Null(pagingParams.Filter);
    }

    [Fact]
    public void Skip_SetValue_ReturnsSetValue()
    {
        // Arrange
        var pagingParams = new PagingParams();

        // Act
        pagingParams.Skip = 50;

        // Assert
        Assert.Equal(50, pagingParams.Skip);
    }

    [Fact]
    public void Take_SetValue_ReturnsSetValue()
    {
        // Arrange
        var pagingParams = new PagingParams();

        // Act
        pagingParams.Take = 25;

        // Assert
        Assert.Equal(25, pagingParams.Take);
    }

    [Fact]
    public void Filter_SetValue_ReturnsSetValue()
    {
        // Arrange
        var pagingParams = new PagingParams();
        const string filter = "name eq 'test'";

        // Act
        pagingParams.Filter = filter;

        // Assert
        Assert.Equal(filter, pagingParams.Filter);
    }

    [Fact]
    public void AllProperties_SetMultipleValues_ReturnsCorrectValues()
    {
        // Arrange & Act
        var pagingParams = new PagingParams
        {
            Skip = 100,
            Take = 50,
            Filter = "status eq 'active'"
        };

        // Assert
        Assert.Equal(100, pagingParams.Skip);
        Assert.Equal(50, pagingParams.Take);
        Assert.Equal("status eq 'active'", pagingParams.Filter);
    }
}
