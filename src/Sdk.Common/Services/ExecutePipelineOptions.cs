namespace Meshmakers.Octo.Sdk.Common.Services;

/// <summary>
/// Options for executing a pipeline
/// </summary>
public class ExecutePipelineOptions(DateTime transactionStartedDateTime, IDictionary<string, object?>? properties = null)
{
    /// <summary>
    /// Gets or sets the date and time when the transaction started
    /// </summary>
    public DateTime TransactionStartedDateTime { get; } = transactionStartedDateTime;

    /// <summary>
    /// Gets or sets the date and time when the transaction started
    /// </summary>
    public DateTime? ExternalReceivedDateTime { get; set; }

    /// <summary>
    /// Context properties that can be used to share data between the adapter and the pipeline
    /// </summary>
    public IDictionary<string, object?>? Properties { get; } = properties;
}