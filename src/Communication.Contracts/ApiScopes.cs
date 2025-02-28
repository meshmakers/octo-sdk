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
    BotApiReadOnly = 32,
    
    /// <summary>
    /// Communication service system API full access
    /// </summary>
    CommunicationServiceSystemApiFullAccess = 64,
    
    /// <summary>
    /// Communication service tenant API full access
    /// </summary>
    CommunicationServiceTenantApiFullAccess = 128,
    
    /// <summary>
    /// Communication service tenant API readonly access
    /// </summary>
    CommunicationServiceTenantApiReadOnly = 256,

    /// <summary>
    /// Reporting service system API full access
    /// </summary>
    ReportingServiceSystemApiFullAccess = 512,

    /// <summary>
    /// Reporting service tenant API readonly access
    /// </summary>
    ReportingServiceTenantApiFullAccess = 1024,

    /// <summary>
    /// Reporting service tenant API readonly access
    /// </summary>
    ReportingServiceTenantApiReadOnly = 2048,
}