using System.ComponentModel.DataAnnotations;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Identity provider configuration specifically for Microsoft Active Directory.
/// </summary>
public class MicrosoftAdProviderDto : IdentityProviderDto
{
#pragma warning disable 1591
    public const int DefaultPort = 636;
#pragma warning restore 1591

    /// <summary>
    ///     Host.
    /// </summary>
    [Required]
    public string Host { get; set; } = null!;

    /// <summary>
    ///     Port (default port 636).
    /// </summary>
    [Range(1, 65535)]
    public ushort Port { get; set; } = DefaultPort;

    /// <summary>
    ///     Whether to use TLS for connecting to the directory server.
    /// </summary>
    public bool UseTls { get; set; }
}