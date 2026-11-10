using GraphQL;

namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants.Methods;

/// <summary>
///     Base class of the generated CK method clients (AB#8423). A generated client (one per CK type with methods,
///     <c>Rt{Type}MethodClient</c>) builds the GraphQL document of each method and sends it through
///     <see cref="ITenantClient.SendMutationAsync{TDto}" />. This class turns the answer into a
///     <see cref="CkMethodResult" />: a success, or one of the errors the method declares; every other error is thrown
///     as <see cref="CkMethodCallException" />, transport errors as before (e.g.
///     <see cref="UnauthorizedServiceAccessException" />, <see cref="ServiceClientException" />).
/// </summary>
public abstract class CkMethodClientBase
{
    /// <summary>
    ///     Creates the client.
    /// </summary>
    /// <param name="tenantClient">The tenant client the calls go through</param>
    protected CkMethodClientBase(ITenantClient tenantClient)
    {
        TenantClient = tenantClient ?? throw new ArgumentNullException(nameof(tenantClient));
    }

    /// <summary>
    ///     The tenant client the calls go through.
    /// </summary>
    protected ITenantClient TenantClient { get; }

    /// <summary>
    ///     Calls a method with a declared result.
    /// </summary>
    /// <param name="methodId">Qualified id of the method (for errors)</param>
    /// <param name="document">The GraphQL document</param>
    /// <param name="operationName">The operation name of the document</param>
    /// <param name="variables">The variables (<c>rtId</c>, <c>input</c>, <c>idempotencyKey</c>)</param>
    /// <param name="declaredErrorCodes">The error codes the method declares</param>
    /// <param name="selectValue">Reads the result value from the response</param>
    /// <typeparam name="TResponse">The generated response type of the method field</typeparam>
    /// <typeparam name="TValue">The C# type of the result</typeparam>
    protected async Task<CkMethodResult<TValue>> InvokeCkMethodAsync<TResponse, TValue>(string methodId, string document,
        string operationName, object variables, IReadOnlyCollection<string> declaredErrorCodes,
        Func<TResponse, TValue?> selectValue)
        where TResponse : CkMethodCallResponse
    {
        ArgumentNullException.ThrowIfNull(selectValue);
        var (response, error) = await SendAsync<TResponse>(methodId, document, operationName, variables,
            declaredErrorCodes);
        return error != null
            ? CkMethodResult<TValue>.Failure(error)
            : CkMethodResult<TValue>.Success(response!.InvocationId, selectValue(response));
    }

    /// <summary>
    ///     Calls a method without a declared result.
    /// </summary>
    /// <param name="methodId">Qualified id of the method (for errors)</param>
    /// <param name="document">The GraphQL document</param>
    /// <param name="operationName">The operation name of the document</param>
    /// <param name="variables">The variables (<c>rtId</c>, <c>input</c>, <c>idempotencyKey</c>)</param>
    /// <param name="declaredErrorCodes">The error codes the method declares</param>
    /// <typeparam name="TResponse">The generated response type of the method field</typeparam>
    protected async Task<CkMethodResult> InvokeCkMethodAsync<TResponse>(string methodId, string document,
        string operationName, object variables, IReadOnlyCollection<string> declaredErrorCodes)
        where TResponse : CkMethodCallResponse
    {
        var (response, error) = await SendAsync<TResponse>(methodId, document, operationName, variables,
            declaredErrorCodes);
        return error != null ? CkMethodResult.Failure(error) : CkMethodResult.Success(response!.InvocationId);
    }

    /// <summary>
    ///     Calls an asynchronous method (<c>execution.mode: async</c>, AB#8446) with a declared result: the call returns
    ///     the operation handle at once; the result is read later with <see cref="CkOperationsClient" />.
    /// </summary>
    /// <param name="methodId">Qualified id of the method (for errors)</param>
    /// <param name="document">The GraphQL document</param>
    /// <param name="operationName">The operation name of the document</param>
    /// <param name="variables">The variables (<c>rtId</c>, <c>input</c>, <c>idempotencyKey</c>)</param>
    /// <param name="declaredErrorCodes">The error codes the method declares</param>
    /// <param name="resultOptions">The JSON options the result is read with; <c>null</c> for the defaults</param>
    /// <typeparam name="TResponse">The generated response type of the method field</typeparam>
    /// <typeparam name="TValue">The C# type of the result</typeparam>
    protected async Task<CkMethodResult<CkOperationHandle<TValue>>> StartCkOperationAsync<TResponse, TValue>(
        string methodId, string document, string operationName, object variables,
        IReadOnlyCollection<string> declaredErrorCodes, global::System.Text.Json.JsonSerializerOptions? resultOptions = null)
        where TResponse : CkMethodAsyncCallResponse
    {
        var (response, error) = await SendAsync<TResponse>(methodId, document, operationName, variables,
            declaredErrorCodes);
        if (error != null)
        {
            return CkMethodResult<CkOperationHandle<TValue>>.Failure(error);
        }

        var operation = RequireOperation(methodId, response!);
        return CkMethodResult<CkOperationHandle<TValue>>.Success(response!.InvocationId,
            new CkOperationHandle<TValue>(operation.OperationId!, methodId, response.InvocationId,
                operation.AcceptedAtUtc, operation.RetryAfterSeconds, operation.ExpiresAtUtc, declaredErrorCodes,
                resultOptions));
    }

    /// <summary>
    ///     Calls an asynchronous method (<c>execution.mode: async</c>, AB#8446) without a declared result, see
    ///     <see cref="StartCkOperationAsync{TResponse,TValue}" />.
    /// </summary>
    /// <param name="methodId">Qualified id of the method (for errors)</param>
    /// <param name="document">The GraphQL document</param>
    /// <param name="operationName">The operation name of the document</param>
    /// <param name="variables">The variables (<c>rtId</c>, <c>input</c>, <c>idempotencyKey</c>)</param>
    /// <param name="declaredErrorCodes">The error codes the method declares</param>
    /// <typeparam name="TResponse">The generated response type of the method field</typeparam>
    protected async Task<CkMethodResult<CkOperationHandle>> StartCkOperationAsync<TResponse>(string methodId,
        string document, string operationName, object variables, IReadOnlyCollection<string> declaredErrorCodes)
        where TResponse : CkMethodAsyncCallResponse
    {
        var (response, error) = await SendAsync<TResponse>(methodId, document, operationName, variables,
            declaredErrorCodes);
        if (error != null)
        {
            return CkMethodResult<CkOperationHandle>.Failure(error);
        }

        var operation = RequireOperation(methodId, response!);
        return CkMethodResult<CkOperationHandle>.Success(response!.InvocationId,
            new CkOperationHandle(operation.OperationId!, methodId, response.InvocationId, operation.AcceptedAtUtc,
                operation.RetryAfterSeconds, operation.ExpiresAtUtc, declaredErrorCodes));
    }

    private static CkOperationHandleResponse RequireOperation(string methodId, CkMethodAsyncCallResponse response) =>
        response.Operation is { OperationId: { Length: > 0 } } operation
            ? operation
            : throw new ServiceClientException(
                $"The asynchronous CK method call '{methodId}' returned no operation handle.");

    private async Task<(TResponse? Response, CkMethodError? Error)> SendAsync<TResponse>(string methodId,
        string document, string operationName, object variables, IReadOnlyCollection<string> declaredErrorCodes)
        where TResponse : CkMethodCallResponse
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(methodId);
        ArgumentException.ThrowIfNullOrWhiteSpace(document);
        ArgumentNullException.ThrowIfNull(variables);
        ArgumentNullException.ThrowIfNull(declaredErrorCodes);

        TResponse? response;
        try
        {
            response = await TenantClient.SendMutationAsync<TResponse>(
                new GraphQLRequest(document, variables, operationName));
        }
        catch (QlQueryErrorException e) when (e is not CkMethodCallException)
        {
            var error = CkMethodErrorReader.Read(e.Errors);
            if (error == null)
            {
                // Not a method error (e.g. a document the schema rejects): unchanged.
                throw;
            }

            if (declaredErrorCodes.Contains(error.Code, StringComparer.Ordinal))
            {
                return (null, error);
            }

            throw new CkMethodCallException(methodId, error, e.Errors, e);
        }

        if (response == null)
        {
            throw new ServiceClientException($"The CK method call '{methodId}' returned no result.");
        }

        return (response, null);
    }
}

/// <summary>
///     The common members of the generated response type of a method field (AB#8423):
///     <c>{ invocationId result }</c>.
/// </summary>
public abstract class CkMethodCallResponse
{
    /// <summary>
    ///     The invocation id of the call.
    /// </summary>
    public string? InvocationId { get; set; }
}
