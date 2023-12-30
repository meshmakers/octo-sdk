namespace Meshmakers.Octo.Common.Shared.DataTransferObjects;

/// <summary>
/// Represents a secret for an API.
/// </summary>
public class ApiSecretDto
{
    /// <summary>
    /// Value of the secret encrypted.
    /// </summary>
    public string ValueEncrypted { get; set; } = null!;
    
    /// <summary>
    /// Value of the secret in clear text.
    /// </summary>
    public string? ValueClearText { get; set; }

    /// <summary>
    /// Expiration date of the secret.
    /// </summary>
    public DateTime? ExpirationDate { get; set; }
    
    /// <summary>
    /// Description of the secret.
    /// </summary>
    public string? Description { get; set; }
}