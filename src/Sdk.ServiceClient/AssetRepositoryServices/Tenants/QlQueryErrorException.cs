using GraphQL;

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

    /// <summary>
    ///     Creates the exception with the GraphQL errors of the response (AB#8423), so a caller can read
    ///     <c>extensions.code</c> and <c>extensions.parameters</c> instead of parsing the message.
    /// </summary>
    /// <param name="message">The message</param>
    /// <param name="errors">The GraphQL errors of the response</param>
    public QlQueryErrorException(string message, IReadOnlyList<GraphQLError> errors) : base(message)
    {
        Errors = errors;
    }

    /// <summary>
    ///     Creates the exception with the GraphQL errors of the response and an inner exception (AB#8423).
    /// </summary>
    /// <param name="message">The message</param>
    /// <param name="errors">The GraphQL errors of the response</param>
    /// <param name="inner">The inner exception</param>
    public QlQueryErrorException(string message, IReadOnlyList<GraphQLError>? errors, Exception? inner)
        : base(message, inner)
    {
        Errors = errors;
    }

    /// <summary>
    ///     The GraphQL errors of the response (with their <c>extensions</c>); <c>null</c> when the exception was not
    ///     created from a GraphQL response (AB#8423).
    /// </summary>
    public IReadOnlyList<GraphQLError>? Errors { get; }
}