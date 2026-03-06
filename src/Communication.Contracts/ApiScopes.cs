namespace Meshmakers.Octo.Communication.Contracts;

/// <summary>
/// API scopes used in the application
/// </summary>
[Flags]
public enum ApiScopes
{
    /// <summary>
    /// no scopes
    /// </summary>
    None = 0,

    /// <summary>
    /// Unified Octo API full access
    /// </summary>
    OctoApiFullAccess = 1,

    /// <summary>
    /// Unified Octo API read-only access
    /// </summary>
    OctoApiReadOnly = 2,
}
