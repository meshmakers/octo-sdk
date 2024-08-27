using Meshmakers.Common.Shared;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Represents a filter value for a near field search
/// </summary>
public class NearGeospatialFilterDto : IGeospatialFilterDto
{
    /// <summary>
    ///     Creates a new instance of <see cref="NearGeospatialFilterDto" />
    /// </summary>
    /// <param name="attributeName">The name of the attribute to compare</param>
    /// <param name="point">Point to search for</param>
    /// <param name="minDistance">The minimum distance from the center point that the documents can be.</param>
    /// <param name="maxDistance">The maximum distance from the center point that the documents can be.</param>
    public NearGeospatialFilterDto(string attributeName, PointDto point, double? minDistance, double? maxDistance)
    {
        ArgumentValidation.ValidateString(nameof(attributeName), attributeName);

        AttributeName = attributeName;
        Point = point;
        MinDistance = minDistance;
        MaxDistance = maxDistance;
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="NearGeospatialFilterDto"/> class.
    /// </summary>
    /// <returns></returns>
    public NearGeospatialFilterDto()
    {
      
    }

    /// <summary>
    /// The point to search for
    /// </summary>
    public PointDto Point { get; set; } = null!;
    
    /// <summary>
    /// The maximum distance from the center point that the documents can be.
    /// </summary>
    public double? MaxDistance { get; set;}
    
    /// <summary>
    /// The minimum distance from the center point that the documents can be
    /// </summary>
    public double? MinDistance { get;  set;}

    /// <inheritdoc />
    public string AttributeName { get; set; } = null!;
}
