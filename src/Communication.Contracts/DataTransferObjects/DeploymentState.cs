namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// State of a deployment
/// </summary>
public enum DeploymentState
{
    /// <summary>
    /// Processing state
    /// </summary>
    Processing = 0,

    /// <summary>
    /// Success state
    /// </summary>
    Success = 1,

    /// <summary>
    /// Deployment failed
    /// </summary>
    Failed = 2,
}