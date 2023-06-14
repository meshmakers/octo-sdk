using System;

namespace Meshmakers.Octo.Sdk.Client;

public interface IServiceClientAccessToken
{
    /// <summary>
    ///     Returns the access token
    /// </summary>
    string? AccessToken { get; set; }

    event EventHandler? AccessTokenUpdated;
}
