using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Identity provider configuration specifically for Microsoft accounts.
/// </summary>
public class MicrosoftIdentityProviderDto : IdentityProviderDto
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

    /// <summary>
    ///     Response only: <c>true</c> when a client secret is stored but cannot be read because its key
    ///     id is not in this environment's key ring (re-entry needed; <see cref="ClientSecretIsSet" />
    ///     is <c>false</c> then). Ignored on create/update. Not written when <c>null</c>.
    /// </summary>
    [JsonPropertyName("clientSecretKeyMissing")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Newtonsoft.Json.JsonProperty("clientSecretKeyMissing", NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    public bool? ClientSecretKeyMissing { get; set; }

    /// <summary>
    ///     Response only: when the current client secret was set (UTC); <c>null</c> when not set or for
    ///     legacy values. Ignored on create/update. Not written when <c>null</c>.
    /// </summary>
    [JsonPropertyName("clientSecretSetAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Newtonsoft.Json.JsonProperty("clientSecretSetAt", NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    public DateTime? ClientSecretSetAt { get; set; }
}
