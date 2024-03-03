namespace Meshmakers.Octo.Sdk.Common.Services;

/// <summary>
/// Options for executing a pipeline
/// </summary>
public class ExecutePipelineOptions(DateTime transactionStartedDateTime)
{
    /// <summary>
    /// Gets or sets the date and time when the transaction started
    /// </summary>
    public DateTime TransactionStartedDateTime { get; } = transactionStartedDateTime;
    
    /// <summary>
    /// Gets or sets the date and time when the transaction started
    /// </summary>
    public DateTime? ExternalReceivedDateTime { get; set; }
}