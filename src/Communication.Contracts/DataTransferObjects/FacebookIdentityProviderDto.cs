using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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
    ///     Client secret. Required on create; on update, omit (null or empty) to preserve the
    ///     existing secret unchanged.
    ///     Write-only: always <c>null</c> in responses; use <see cref="ClientSecretIsSet" /> to
    ///     find out whether a secret is stored.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    ///     Response only: whether a client secret is stored for this identity provider. The secret
    ///     itself is write-only and never returned (<see cref="ClientSecret" /> is always <c>null</c>
    ///     in responses). Ignored on create/update. Not written when <c>null</c>.
    /// </summary>
    [JsonPropertyName("clientSecretIsSet")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Newtonsoft.Json.JsonProperty("clientSecretIsSet", NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    public bool? ClientSecretIsSet { get; set; }
}
