using System.Runtime.Serialization;

namespace Meshmakers.Octo.Sdk.ServiceClient;

/// <summary>
/// Exception thrown when a service client fails to access a service.
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
}
