namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Defines deployment error categories for adapters
/// </summary>
public enum DeploymentErrorCategories
{
    /// <summary>
    /// An error with no specific category
    /// </summary>
    Uncategorized = 0,

    /// <summary>
    /// Pipeline initialization error during deployment
    /// </summary>
    PipelineInitializationError = 1,

    /// <summary>
    /// An error during pipeline deserialization
    /// </summary>
    PipelineDeserializationError = 2,

    /// <summary>
    /// An error occurred during pipeline trigger execution of Startup or Shutdown
    /// </summary>
    PipelineTriggerExecutionError = 3,

}