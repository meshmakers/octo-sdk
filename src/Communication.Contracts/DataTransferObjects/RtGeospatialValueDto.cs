using Meshmakers.Octo.Runtime.Contracts.Geospatial.Geometry;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Represents a geospatial value of  
/// </summary>
public class RtGeospatialValueDto
{
    /// <summary>
    /// The geographical distance if a geospatial query has been applied
    /// </summary>
    public double? Distance { get; set; }

    /// <summary>
    /// Gets the geospatial point
    /// </summary>
    public Point Point { get; set; } = null!;
}