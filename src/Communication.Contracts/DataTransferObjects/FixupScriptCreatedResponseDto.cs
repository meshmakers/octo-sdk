namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Represents a response for a fixup script creation request.
/// </summary>
public class FixupScriptCreatedResponseDto
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="jobId"></param>
    public FixupScriptCreatedResponseDto(string jobId)
    {
        JobId = jobId;
    }

    /// <summary>
    ///     Gets or sets the job id.
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string JobId { get; set; }
}