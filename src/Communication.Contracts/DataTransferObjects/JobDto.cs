// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Represents a job for data transfer.
/// </summary>
public class JobDto
{
    /// <summary>
    ///     Gets or sets the job id
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    ///     Datetime of job creation
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    ///     Datetime of last state change
    /// </summary>
    public DateTime? StateChangedAt { get; set; }

    /// <summary>
    ///     Current status
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    ///     Reason of the current state
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the error message if the job failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}