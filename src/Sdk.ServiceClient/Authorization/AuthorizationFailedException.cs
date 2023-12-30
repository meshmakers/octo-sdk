namespace Meshmakers.Octo.Common.Shared.Authorization;

/// <summary>
/// Represents an exception thrown when authorization failed.
/// </summary>
[Serializable]
public class AuthorizationFailedException : Exception
{
    /// <inheritdoc />
    public AuthorizationFailedException(string message) : base(message)
    {
    }

    /// <inheritdoc />
    public AuthorizationFailedException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }


    internal static Exception AuthenticationFailed(string? responseError, Exception? responseException)
    {
        return new AuthorizationFailedException(
            $"Authentication failed. Response error: {responseError}", responseException);
    }
}