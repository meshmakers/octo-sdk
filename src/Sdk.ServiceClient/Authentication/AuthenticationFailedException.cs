using System;
using System.Runtime.Serialization;

namespace Meshmakers.Octo.Sdk.ServiceClient.Authentication;

/// <summary>
/// Exception thrown when the authentication failed.
/// </summary>
[Serializable]
public class AuthenticationFailedException : Exception
{
    /// <inheritdoc />
    public AuthenticationFailedException()
    {
    }

    /// <inheritdoc />
    public AuthenticationFailedException(string message) : base(message)
    {
    }

    /// <inheritdoc />
    public AuthenticationFailedException(string? message, Exception? inner) : base(message, inner)
    {
    }

    /// <inheritdoc />
    protected AuthenticationFailedException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }

    internal static Exception RequestFailed(string? responseError, Exception? responseException)
    {
        return new AuthenticationFailedException($"Authentication request failed with message: {responseError}", responseException);
    }
}
