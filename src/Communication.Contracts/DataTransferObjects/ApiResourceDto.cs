namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Represents an API resource.
/// </summary>
public class ApiResourceDto
{
    /// <summary>
    /// Gets or sets if the API resource is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the API resource.
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Gets or sets the display name of the API resource.
    /// </summary>
    public string? DisplayName { get; set; }
    
    /// <summary>
    /// Gets or sets the description of the API resource.
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
    /// Gets or sets a value indicating whether this scope is a resource indicator.
    /// </summary>
    public bool RequireResourceIndicator { get; set; }
    
    /// <summary>
    /// Gets or sets a collection of scopes that the client is allowed to request.
    /// </summary>
    public ICollection<string>? Scopes { get; set; }
    
    /// <summary>
    /// Gets or sets a collection of allowed access token signing algorithms.
    /// </summary>
    public ICollection<string>? AllowedAccessTokenSigningAlgorithms { get; set; }
}