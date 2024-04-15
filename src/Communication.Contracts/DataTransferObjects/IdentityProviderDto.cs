using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Meshmakers.Octo.ConstructionKit.Contracts;
using static Meshmakers.Octo.Communication.Contracts.DataTransferObjects.ValidationConstants;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Data Transfer Object of a Octo identity provider
/// </summary>
[JsonPolymorphic]
[JsonDerivedType(typeof(FacebookIdentityProviderDto), (int)IdentityProviderTypesDto.Facebook)]
[JsonDerivedType(typeof(GoogleIdentityProviderDto), (int)IdentityProviderTypesDto.Google)]
[JsonDerivedType(typeof(MicrosoftIdentityProviderDto), (int)IdentityProviderTypesDto.Microsoft)]
[JsonDerivedType(typeof(AzureEntraIdProviderDto), (int)IdentityProviderTypesDto.MicrosoftAzureAd)]
[JsonDerivedType(typeof(MicrosoftAdProviderDto), (int)IdentityProviderTypesDto.MicrosoftActiveDirectory)]
[JsonDerivedType(typeof(OpenLdapProviderDto), (int)IdentityProviderTypesDto.OpenLdap)]
public class IdentityProviderDto
{
    /// <summary>
    ///     Indicates if an identity provider is enabled
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public bool IsEnabled { get; set; }

    /// <summary>
    ///     Unique ID for the IdentityProviderConfiguration. Do not set this property when creating a new configuration.
    ///     The API automatically returns an ID once the configuration has been created.
    /// </summary>
    [StringLength(TextDefaultMaxLength)]
    public OctoObjectId? RtId { get; set; }

    /// <summary>
    ///     Free definable for all different identity provider types.
    /// </summary>
    [Required]
    [StringLength(NameMaxLength, MinimumLength = NameMinLength)]
    public string? Name { get; set; }

    /// <summary>
    ///     An arbitrary long text describing the identity provider configuration in detail.
    /// </summary>
    [StringLength(DescriptionDefaultMaxLength)]
    public string? Description { get; set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public const int NameMinLength = 3;
    public const int NameMaxLength = TextDefaultMaxLength;

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}