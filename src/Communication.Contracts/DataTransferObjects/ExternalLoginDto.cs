namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Represents an external login provider linked to a user.
/// </summary>
public class ExternalLoginDto
{
    /// <summary>
    ///     Gets or sets the login provider name (e.g., "Google", "Microsoft").
    /// </summary>
    public string LoginProvider { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the display name of the provider.
    /// </summary>
    public string ProviderDisplayName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the provider-specific user key.
    /// </summary>
    public string ProviderKey { get; set; } = string.Empty;
}
