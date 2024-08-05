namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Gets or sets the export model response.
/// </summary>
public class ExportModelResponseDto
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="jobId"></param>
    public ExportModelResponseDto(string jobId)
    {
        JobId = jobId;
    }

    /// <summary>
    ///     Gets or sets the job id.
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string JobId { get; set; }
}