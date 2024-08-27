namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// A position is the fundamental geometry construct, consisting of Latitude, Longitude and (optionally) Altitude.
/// </summary>
public class PositionDto
{
    /// <summary>
    /// Gets the altitude.
    /// </summary>
    public double? Altitude { get; set; }

    /// <summary>
    /// Gets the latitude or Y coordinate
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Gets the longitude or X coordinate
    /// </summary>
    public double Longitude { get; set; }
}