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
    /// Asset repository API full access
    /// </summary>
    AssetTenantApiFullAccess = 4,

    /// <summary>
    /// Asset repository API readonly access
    /// </summary>
    AssetTenantApiReadOnly = 8,
    
    /// <summary>
    /// Identity API full access
    /// </summary>
    IdentityApiFullAccess = 16,
    
    /// <summary>
    /// Identity API readonly access
    /// </summary>
    IdentityApiReadOnly = 32,
    
    /// <summary>
    /// Bot API full access
    /// </summary>
    BotApiFullAccess = 64,
    
    /// <summary>
    /// Bot API readonly access
    /// </summary>
    BotApiReadOnly = 128,
    
    /// <summary>
    /// Communication service system API full access
    /// </summary>
    CommunicationServiceSystemApiFullAccess = 256,
    
    /// <summary>
    /// Communication service tenant API full access
    /// </summary>
    CommunicationServiceTenantApiFullAccess = 512,
    
    /// <summary>
    /// Communication service tenant API readonly access
    /// </summary>
    CommunicationServiceTenantApiReadOnly = 1024,

    /// <summary>
    /// Reporting service system API full access
    /// </summary>
    ReportingServiceSystemApiFullAccess = 2048,

    /// <summary>
    /// Reporting service tenant API readonly access
    /// </summary>
    ReportingServiceTenantApiFullAccess = 4096,

    /// <summary>
    /// Reporting service tenant API readonly access
    /// </summary>
    ReportingServiceTenantApiReadOnly = 8096,
}