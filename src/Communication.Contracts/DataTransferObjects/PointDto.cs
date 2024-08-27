namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Defines the Point type. In geography, a point refers to a Position on a map, expressed in latitude and longitude. 
/// </summary>
public class PointDto
{
    /// <summary>
    /// Creates a new instance of the <see cref="PointDto"/> class.
    /// </summary>
    /// <param name="coordinates">The Position.</param>
    public PointDto(PositionDto coordinates) => Coordinates = coordinates;
    
    /// <summary>
    /// The Position underlying this point.
    /// </summary>
    public PositionDto Coordinates { get; set; }
}