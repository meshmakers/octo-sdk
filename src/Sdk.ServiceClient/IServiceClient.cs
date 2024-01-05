namespace Meshmakers.Octo.Sdk.ServiceClient;

/// <summary>
///     Base interface of REST based service clients.
/// </summary>
public interface IServiceClient
{
    /// <summary>
    ///     Returns the access token used to authenticate the client.
    /// </summary>
    IServiceClientAccessToken AccessToken { get; }

    /// <summary>
    ///     Returns the service URI of the REST service.
    /// </summary>
    Uri ServiceUri { get; }
}