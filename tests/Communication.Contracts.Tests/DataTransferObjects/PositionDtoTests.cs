using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

namespace Communication.Contracts.Tests.DataTransferObjects;

public class PositionDtoTests
{
    [Fact]
    public void Latitude_SetValue_ReturnsSetValue()
    {
        // Arrange
        var position = new PositionDto();

        // Act
        position.Latitude = 48.8566;

        // Assert
        Assert.Equal(48.8566, position.Latitude);
    }

    [Fact]
    public void Longitude_SetValue_ReturnsSetValue()
    {
        // Arrange
        var position = new PositionDto();

        // Act
        position.Longitude = 2.3522;

        // Assert
        Assert.Equal(2.3522, position.Longitude);
    }

    [Fact]
    public void Altitude_SetValue_ReturnsSetValue()
    {
        // Arrange
        var position = new PositionDto();

        // Act
        position.Altitude = 35.0;

        // Assert
        Assert.Equal(35.0, position.Altitude);
    }

    [Fact]
    public void Altitude_NotSet_ReturnsNull()
    {
        // Arrange & Act
        var position = new PositionDto();

        // Assert
        Assert.Null(position.Altitude);
    }

    [Fact]
    public void AllProperties_SetValues_ReturnsCorrectValues()
    {
        // Arrange & Act
        var position = new PositionDto
        {
            Latitude = 51.5074,
            Longitude = -0.1278,
            Altitude = 11.0
        };

        // Assert
        Assert.Equal(51.5074, position.Latitude);
        Assert.Equal(-0.1278, position.Longitude);
        Assert.Equal(11.0, position.Altitude);
    }

    [Fact]
    public void Position_NegativeCoordinates_AcceptsValues()
    {
        // Arrange & Act
        var position = new PositionDto
        {
            Latitude = -33.8688,
            Longitude = 151.2093
        };

        // Assert
        Assert.Equal(-33.8688, position.Latitude);
        Assert.Equal(151.2093, position.Longitude);
    }
}
