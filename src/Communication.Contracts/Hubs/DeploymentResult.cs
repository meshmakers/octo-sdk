using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

namespace Meshmakers.Octo.Communication.Contracts.Hubs;

/// <summary>
/// Result of a deployment
/// </summary>
public class DeploymentResult
{
    /// <summary>
    /// Indicates if the deployment was successful
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// Error message in case of failure
    /// </summary>
    public IEnumerable<DeploymentUpdateErrorMessageDto>? ErrorMessages { get; set; }
}