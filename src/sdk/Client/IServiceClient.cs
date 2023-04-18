using System;

namespace Meshmakers.Octo.Frontend.Client;

public interface IServiceClient
{
    IServiceClientAccessToken AccessToken { get; }

    Uri ServiceUri { get; }

    void Initialize();
}
