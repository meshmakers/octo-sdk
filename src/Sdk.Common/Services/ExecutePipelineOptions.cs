using Meshmakers.Octo.ConstructionKit.Contracts;

namespace Meshmakers.Octo.Sdk.Common.Services;

/// <summary>
/// Options for executing a pipeline
/// </summary>
public class ExecutePipelineOptions(
    DateTime transactionStartedDateTime,
    Func<OctoObjectId, string, Task> sendDebugInfoFunc)
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
    ///     Sends debug information to the backend
    /// </summary>
    public Func<OctoObjectId, string, Task> SendDebugInfoFunc { get; init; } = sendDebugInfoFunc;
}