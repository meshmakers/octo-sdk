namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Represents a response including the job id of a long-running process
/// </summary>
public class JobResponseDto
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="jobId"></param>
    public JobResponseDto(string jobId)
    {
        JobId = jobId;
    }

    /// <summary>
    ///     Gets or sets the job id.
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string JobId { get; set; }
}