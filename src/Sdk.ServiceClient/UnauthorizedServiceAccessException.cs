using System;
using System.Runtime.Serialization;

namespace Meshmakers.Octo.Sdk.ServiceClient;

/// <summary>
/// Exception thrown when a service client fails to access a service.
/// </summary>
[Serializable]
public class UnauthorizedServiceAccessException : ServiceClientException
{
    /// <inheritdoc />
    public UnauthorizedServiceAccessException()
    {
    }

    /// <inheritdoc />
    public UnauthorizedServiceAccessException(Exception? inner)
        : this("Access to the requested resource is not allowed.", inner)
    {
    }

    /// <inheritdoc />
    public UnauthorizedServiceAccessException(string message) : base(message)
    {
    }

    /// <inheritdoc />
    public UnauthorizedServiceAccessException(string message, Exception? inner) : base(message, inner)
    {
    }

    /// <inheritdoc />
    protected UnauthorizedServiceAccessException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}
