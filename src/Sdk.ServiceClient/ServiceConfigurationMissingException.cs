namespace Meshmakers.Octo.Sdk.ServiceClient;

/// <summary>
///     Exception thrown when a service configuration is missing.
/// </summary>
[Serializable]
public class ServiceConfigurationMissingException : Exception
{
    /// <inheritdoc />
    public ServiceConfigurationMissingException()
    {
    }

    /// <inheritdoc />
    public ServiceConfigurationMissingException(string message) : base(message)
    {
    }

    /// <inheritdoc />
    public ServiceConfigurationMissingException(string message, Exception inner) : base(message, inner)
    {
    }
}