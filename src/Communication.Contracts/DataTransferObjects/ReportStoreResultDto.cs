using Meshmakers.Octo.ConstructionKit.Contracts;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Represents the result of a report generation request.
/// </summary>
public class ReportStoreResultDto
{
    /// <summary>
    /// Returns the unique identifier of the generated report
    /// </summary>
    public OctoObjectId FileSystemRtId { get; set; } = OctoObjectId.Empty;

    /// <summary>
    /// Returns the uri of the generated report
    /// </summary>
    public string ReportUri { get; set; } = string.Empty;
}