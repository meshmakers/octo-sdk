using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

namespace Communication.Contracts.Tests.DataTransferObjects;

public class PointDtoTests
{
    [Fact]
    public void Constructor_WithCoordinates_SetsCoordinates()
    {
        // Arrange
        var coordinates = new PositionDto
        {
            Latitude = 48.8566,
            Longitude = 2.3522
        };

        // Act
        var point = new PointDto(coordinates);

        // Assert
        Assert.Same(coordinates, point.Coordinates);
        Assert.Equal(48.8566, point.Coordinates.Latitude);
        Assert.Equal(2.3522, point.Coordinates.Longitude);
    }

    [Fact]
    public void Coordinates_SetNewValue_ReturnsNewValue()
    {
        // Arrange
        var initialCoordinates = new PositionDto { Latitude = 0, Longitude = 0 };
        var point = new PointDto(initialCoordinates);

        var newCoordinates = new PositionDto
        {
            Latitude = 51.5074,
            Longitude = -0.1278
        };

        // Act
        point.Coordinates = newCoordinates;

        // Assert
        Assert.Same(newCoordinates, point.Coordinates);
    }

    [Fact]
    public void Point_WithAltitude_PreservesAltitude()
    {
        // Arrange
        var coordinates = new PositionDto
        {
            Latitude = 27.9881,
            Longitude = 86.9250,
            Altitude = 8848.86
        };

        // Act
        var point = new PointDto(coordinates);

        // Assert
        Assert.Equal(8848.86, point.Coordinates.Altitude);
    }
}
