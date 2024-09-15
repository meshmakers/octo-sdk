using System.ComponentModel.DataAnnotations;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Generic error response
/// </summary>
public class ErrorResponse
{
    /// <summary>
    ///     Error message that informs about the error.
    /// </summary>
    [Required]
    public string? ErrorMessage { get; set; }
}