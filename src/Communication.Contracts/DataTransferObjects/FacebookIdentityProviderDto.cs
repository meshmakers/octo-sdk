using System.ComponentModel.DataAnnotations;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Identity provider configuration specifically for Facebook accounts.
/// </summary>
public class FacebookIdentityProviderDto : IdentityProviderDto
{
    /// <summary>
    ///     client id
    /// </summary>
    [Required]
    public string? ClientId { get; set; }

    /// <summary>
    ///     client secret
    /// </summary>
    [Required]
    public string? ClientSecret { get; set; }
}