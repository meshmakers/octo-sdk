using System;

namespace Meshmakers.Octo.Sdk.ServiceClient;

public interface IServiceClient
{
    IServiceClientAccessToken AccessToken { get; }

    Uri? ServiceUri { get; }
}
