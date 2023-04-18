using System;

namespace Meshmakers.Octo.Frontend.Client;

public interface IServiceClientAccessToken
{
    /// <summary>
    ///     Returns the access token
    /// </summary>
    string AccessToken { get; set; }

    event EventHandler AccessTokenUpdated;
}
