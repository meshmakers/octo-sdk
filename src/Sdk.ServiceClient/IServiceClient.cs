using System;

namespace Meshmakers.Octo.Sdk.Client;

public interface IServiceClient
{
    IServiceClientAccessToken? AccessToken { get; }

    Uri? ServiceUri { get; }
}
