namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Represents an API scope.
/// </summary>
public class ApiScopeDto
{
    /// <summary>
    /// Gets or set if the scope is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the scope.
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Gets or sets the display name of the scope.
    /// </summary>
    public string? DisplayName { get; set; }
    
    /// <summary>
    /// Gets or sets the description of the scope.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the scope is shown in the discovery document.
    /// </summary>
    public bool ShowInDiscoveryDocument { get; set; }
    
    /// <summary>
    /// Gets or sets a list of user claims that should be included in the access token.
    /// </summary>
    public ICollection<string>? UserClaims { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether this scope is required.
    /// </summary>
    public bool IsRequired { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether this scope is emphasize.
    /// </summary>
    public bool IsEmphasize { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether this scope is a resource indicator.
    /// </summary>
    public bool RequireResourceIndicator { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating if api secrets are required for this scope.
    /// </summary>
    public bool ApiSecrets { get; set; }
}