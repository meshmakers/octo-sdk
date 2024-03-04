namespace Meshmakers.Octo.Sdk.ServiceClient;

/// <summary>
///     Exception thrown when a service client fails to access a service.
/// </summary>
[Serializable]
public class ServiceClientException : Exception
{
    /// <inheritdoc />
    public ServiceClientException()
    {
    }

    /// <inheritdoc />
    public ServiceClientException(string message) : base(message)
    {
    }

    /// <inheritdoc />
    public ServiceClientException(string? message, Exception? inner) : base(message, inner)
    {
    }

    internal static Exception NotConnected()
    {
        throw new ServiceClientException("Not connected to the service.");
    }

    internal static Exception ReconnectAlreadyEnabled()
    {
        throw new ServiceClientException("Reconnect is already enabled.");
    }
}