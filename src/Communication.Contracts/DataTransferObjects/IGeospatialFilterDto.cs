
namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Interface for a geospatial filter
/// </summary>
public interface IGeospatialFilterDto
{
    /// <summary>
    /// The attribute name to search for
    /// </summary>
    public string AttributeName { get; set; }
}