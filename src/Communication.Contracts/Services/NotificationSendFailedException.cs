namespace Meshmakers.Octo.Communication.Contracts.Services;

/// <summary>
/// Thrown when a notification could not be sent.
/// </summary>
[Serializable]
public class NotificationSendFailedException : Exception
{
    /// <inheritdoc />
    public NotificationSendFailedException()
    {
    }

    /// <inheritdoc />
    public NotificationSendFailedException(string message) : base(message)
    {
    }

    /// <inheritdoc />
    public NotificationSendFailedException(string message, Exception inner) : base(message, inner)
    {
    }
}