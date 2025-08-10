namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects.ApiErrors;

/// <summary>
///     Implements optional details to the failed operation
/// </summary>
public class FailedDetailsDto
{
    /// <summary>
    ///     The message code
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    ///     Description to the code
    /// </summary>
    public string? Description { get; set; }
}