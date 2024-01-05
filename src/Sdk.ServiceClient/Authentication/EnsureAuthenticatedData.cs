// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Meshmakers.Octo.Sdk.ServiceClient.Authentication;

/// <summary>
///     Represents the data returned by the EnsureAuthenticated method.
/// </summary>
public class EnsureAuthenticatedData
{
    /// <summary>
    ///     Returns true, if the refresh of the access token was done
    /// </summary>
    public bool IsRefreshDone { get; set; }

    /// <summary>
    ///     Returns the refreshed authentication data.
    /// </summary>
    public AuthenticationData RefreshedAuthenticationData { get; set; } = null!;
}