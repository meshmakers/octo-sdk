using System;

namespace Meshmakers.Octo.Sdk.ServiceClient;

/// <summary>
///    Interface for a service client that uses an access token
/// </summary>
public interface IServiceClientAccessToken
{
    /// <summary>
    ///     Returns the access token
    /// </summary>
    string? AccessToken { get; set; }

    /// <summary>
    ///    Event raised when the access token is updated
    /// </summary>
    event EventHandler? AccessTokenUpdated;
}
