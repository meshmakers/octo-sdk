namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Information about all available identity providers.
/// </summary>
public class IdentityProvidersResult
{
    /// <summary>
    ///     The available identity providers.
    /// </summary>
    public IEnumerable<IdentityProviderDto>? IdentityProviders { get; set; }
}