namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Gets or sets the transfer (for export and import) model response.
/// </summary>
public class TransferModelResponseDto
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="jobId"></param>
    public TransferModelResponseDto(string jobId)
    {
        JobId = jobId;
    }

    /// <summary>
    ///     Gets or sets the job id.
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string JobId { get; set; }
}