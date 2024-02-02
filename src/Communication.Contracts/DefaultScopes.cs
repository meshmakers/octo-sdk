namespace Meshmakers.Octo.Communication.Contracts;

/// <summary>
/// Default scopes used in the application
/// </summary>
[Flags]
public enum DefaultScopes
{
    /// <summary>
    /// No scopes
    /// </summary>
    None = 0,
    
    /// <summary>
    /// OpenId scope
    /// </summary>
    OpenId = 1,
    
    /// <summary>
    /// Profile scope
    /// </summary>
    Profile = 2,
    
    /// <summary>
    /// Email scope
    /// </summary>
    Email = 4,
    
    /// <summary>
    /// Role scope
    /// </summary>
    Role = 8,
    
    /// <summary>
    /// Offline access scope
    /// </summary>
    OfflineAccess = 16,

    /// <summary>
    /// User default scopes
    /// </summary>
    UserDefault = OpenId | Profile | Email | Role
}