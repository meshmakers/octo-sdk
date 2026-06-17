using System.ComponentModel.DataAnnotations;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Identity provider configuration specifically for Google accounts.
/// </summary>
public class GoogleIdentityProviderDto : IdentityProviderDto
{
    /// <summary>
    ///     client id
    /// </summary>
    [Required]
    public string? ClientId { get; set; }

    /// <summary>
    ///     Client secret. Required on create; on update, omit (null or empty) to preserve the
    ///     existing secret unchanged.
    /// </summary>
    public string? ClientSecret { get; set; }
}