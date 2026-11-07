using GraphQL;

namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants.Methods;

/// <summary>
///     A CK method call failed with an error the method does not declare (AB#8423): a platform error such as
///     <c>METHOD_FORBIDDEN</c>, <c>METHOD_VALIDATION_FAILED</c>, <c>METHOD_TARGET_NOT_FOUND</c>, <c>METHOD_TIMEOUT</c> or
///     <c>METHOD_HANDLER_FAILED</c>. It is a <see cref="QlQueryErrorException" />, so code that catches GraphQL errors
///     today keeps working; <see cref="Error" /> carries the code and parameters.
/// </summary>
[Serializable]
public class CkMethodCallException : QlQueryErrorException
{
    /// <summary>
    ///     Creates the exception.
    /// </summary>
    /// <param name="methodId">The qualified id of the called method</param>
    /// <param name="error">The error the service answered</param>
    /// <param name="errors">All GraphQL errors of the response</param>
    /// <param name="inner">The exception the GraphQL client threw</param>
    public CkMethodCallException(string methodId, CkMethodError error, IReadOnlyList<GraphQLError>? errors,
        Exception? inner)
        : base($"The CK method call '{methodId}' failed: {error.Code}", errors, inner)
    {
        MethodId = methodId;
        Error = error;
    }

    /// <summary>
    ///     The qualified id of the called method, e.g. <c>System.Identity/User.ResetPassword-1</c>.
    /// </summary>
    public string MethodId { get; }

    /// <summary>
    ///     The error code and parameters the service answered.
    /// </summary>
    public CkMethodError Error { get; }

    /// <summary>
    ///     Shortcut for <see cref="CkMethodError.Code" /> of <see cref="Error" />.
    /// </summary>
    public string Code => Error.Code;
}
