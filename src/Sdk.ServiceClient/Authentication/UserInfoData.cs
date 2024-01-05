using System.Security.Claims;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Meshmakers.Octo.Sdk.ServiceClient.Authentication;

/// <summary>
///     Represents the user info data.
/// </summary>
// ReSharper disable once UnusedType.Global
public class UserInfoData
{
    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="isAuthenticated">True, if the user is authenticated</param>
    /// <param name="claims">A list of claim received</param>
    public UserInfoData(bool isAuthenticated, IEnumerable<Claim> claims)
    {
        IsAuthenticated = isAuthenticated;
        Claims = claims;
    }

    /// <summary>
    ///     Gets the claims received
    /// </summary>
    public IEnumerable<Claim> Claims { get; }

    /// <summary>
    ///     True, if the user is authenticated
    /// </summary>
    public bool IsAuthenticated { get; }
}