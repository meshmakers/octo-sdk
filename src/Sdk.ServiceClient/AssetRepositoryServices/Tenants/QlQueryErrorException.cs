namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;

/// <summary>
///     Exception thrown when a QL query fails.
/// </summary>
[Serializable]
public class QlQueryErrorException : Exception
{
    /// <inheritdoc />
    public QlQueryErrorException()
    {
    }

    /// <inheritdoc />
    public QlQueryErrorException(string message) : base(message)
    {
    }

    /// <inheritdoc />
    public QlQueryErrorException(string message, Exception inner) : base(message, inner)
    {
    }
}