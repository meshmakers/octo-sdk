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
    /// Asset repository API full access
    /// </summary>
    AssetSystemApiFullAccess = 1,
    
    /// <summary>
    /// Asset repository API readonly access
    /// </summary>
    AssetSystemApiReadOnly = 2,
    
    /// <summary>
    /// Identity API full access
    /// </summary>
    IdentityApiFullAccess = 4,
    
    /// <summary>
    /// Identity API readonly access
    /// </summary>
    IdentityApiReadOnly = 8,
    
    /// <summary>
    /// Bot API full access
    /// </summary>
    BotApiFullAccess = 16,
    
    /// <summary>
    /// Bot API readonly access
    /// </summary>
    BotApiReadOnly = 32
}