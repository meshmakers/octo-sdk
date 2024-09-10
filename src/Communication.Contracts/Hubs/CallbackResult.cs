namespace Meshmakers.Octo.Communication.Contracts.Hubs;

/// <summary>
/// Result of a callback
/// </summary>
public class CallbackResult
{
    /// <summary>
    /// Indicates if the callback was successful
    /// </summary>
    public bool IsSuccess { get; set; }
    /// <summary>
    /// Error message in case of failure
    /// </summary>
    public string? ErrorMessage { get; set; }
}