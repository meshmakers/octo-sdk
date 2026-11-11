using System.Text.Json;
using System.Text.Json.Serialization;

namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants.Methods;

/// <summary>
///     The handle of an accepted call of an asynchronous CK method (<c>execution.mode: async</c>, AB#8446). The call
///     returns at once; state, progress and the result are read later with <see cref="CkOperationsClient" />
///     (<see cref="CkOperationsClient.GetOperationAsync" />, <see cref="CkOperationsClient.WaitForOperationAsync(CkOperationHandle, IProgress{CkOperation}?, CkOperationWaitOptions?, CancellationToken)" />).
///     The wire fields follow the method wire contract (AB#6874): <c>operationId</c>, <c>acceptedAtUtc</c>,
///     <c>retryAfterSeconds</c>, <c>expiresAtUtc</c>.
/// </summary>
public class CkOperationHandle
{
    /// <summary>
    ///     Creates a handle.
    /// </summary>
    /// <param name="operationId">The opaque id of the operation</param>
    /// <param name="methodId">The qualified id of the method that started it; <c>null</c> when unknown</param>
    /// <param name="invocationId">The invocation id of the call that started it; <c>null</c> when unknown</param>
    /// <param name="acceptedAtUtc">The time (UTC) the operation was accepted</param>
    /// <param name="retryAfterSeconds">The suggested delay before the first status check, in seconds</param>
    /// <param name="expiresAtUtc">The time (UTC) after which the operation may be forgotten; <c>null</c> while it is open</param>
    /// <param name="declaredErrorCodes">The error codes the method declares; a failed operation with one of them is a result, not an exception</param>
    public CkOperationHandle(string operationId, string? methodId = null, string? invocationId = null,
        DateTime? acceptedAtUtc = null, int? retryAfterSeconds = null, DateTime? expiresAtUtc = null,
        IReadOnlyCollection<string>? declaredErrorCodes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        OperationId = operationId;
        MethodId = methodId;
        InvocationId = invocationId;
        AcceptedAtUtc = acceptedAtUtc;
        RetryAfterSeconds = retryAfterSeconds;
        ExpiresAtUtc = expiresAtUtc;
        DeclaredErrorCodes = declaredErrorCodes ?? [];
    }

    /// <summary>
    ///     The opaque id of the operation, unique in the tenant. Keep it to read the operation again later (e.g. after a
    ///     restart of the client); <c>new CkOperationHandle(operationId)</c> recreates the handle.
    /// </summary>
    public string OperationId { get; }

    /// <summary>
    ///     The qualified id of the method that started the operation, e.g. <c>System.Bot/TenantRepository.Dump-1</c>;
    ///     <c>null</c> for a handle created from an id only.
    /// </summary>
    public string? MethodId { get; }

    /// <summary>
    ///     The invocation id of the call that started the operation (correlation with the handler log).
    /// </summary>
    public string? InvocationId { get; }

    /// <summary>
    ///     The time (UTC) the operation was accepted.
    /// </summary>
    public DateTime? AcceptedAtUtc { get; }

    /// <summary>
    ///     The suggested delay before the first status check, in seconds; <see cref="CkOperationsClient" /> waits at least
    ///     this long between two reads.
    /// </summary>
    public int? RetryAfterSeconds { get; }

    /// <summary>
    ///     The time (UTC) after which the platform may forget the operation; <c>null</c> while it is open.
    /// </summary>
    public DateTime? ExpiresAtUtc { get; }

    /// <summary>
    ///     The error codes the method declares in its CK model. A failed operation with one of these codes is returned as
    ///     <see cref="CkMethodResult.Error" /> by <see cref="CkOperationsClient.WaitForOperationAsync(CkOperationHandle, IProgress{CkOperation}?, CkOperationWaitOptions?, CancellationToken)" />;
    ///     every other code is thrown as <see cref="CkMethodCallException" />.
    /// </summary>
    public IReadOnlyCollection<string> DeclaredErrorCodes { get; }

    /// <inheritdoc />
    public override string ToString() => OperationId;
}

/// <summary>
///     The handle of an accepted call of an asynchronous CK method with a declared result of type
///     <typeparamref name="TResult" /> (AB#8446), see <see cref="CkOperationHandle" />.
/// </summary>
/// <typeparam name="TResult">The C# type of the declared result</typeparam>
public sealed class CkOperationHandle<TResult> : CkOperationHandle
{
    private readonly JsonSerializerOptions _resultOptions;

    /// <summary>
    ///     Creates a handle.
    /// </summary>
    /// <param name="operationId">The opaque id of the operation</param>
    /// <param name="methodId">The qualified id of the method that started it; <c>null</c> when unknown</param>
    /// <param name="invocationId">The invocation id of the call that started it; <c>null</c> when unknown</param>
    /// <param name="acceptedAtUtc">The time (UTC) the operation was accepted</param>
    /// <param name="retryAfterSeconds">The suggested delay before the first status check, in seconds</param>
    /// <param name="expiresAtUtc">The time (UTC) after which the operation may be forgotten; <c>null</c> while it is open</param>
    /// <param name="declaredErrorCodes">The error codes the method declares</param>
    /// <param name="resultOptions">The JSON options the result is read with; <c>null</c> for <see cref="CkOperationResultJson.DefaultOptions" /></param>
    public CkOperationHandle(string operationId, string? methodId = null, string? invocationId = null,
        DateTime? acceptedAtUtc = null, int? retryAfterSeconds = null, DateTime? expiresAtUtc = null,
        IReadOnlyCollection<string>? declaredErrorCodes = null, JsonSerializerOptions? resultOptions = null)
        : base(operationId, methodId, invocationId, acceptedAtUtc, retryAfterSeconds, expiresAtUtc, declaredErrorCodes)
    {
        _resultOptions = resultOptions ?? CkOperationResultJson.DefaultOptions;
    }

    /// <summary>
    ///     Reads the result of a succeeded operation as <typeparamref name="TResult" />.
    /// </summary>
    /// <param name="operation">The succeeded operation</param>
    /// <returns>The typed result; <c>default</c> when the operation has no result</returns>
    public TResult? ReadResult(CkOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return operation.Result is { ValueKind: not JsonValueKind.Null and not JsonValueKind.Undefined } result
            ? result.Deserialize<TResult>(_resultOptions)
            : default;
    }
}

/// <summary>
///     The JSON options the result of an operation is read with (AB#8446). The result arrives in the JSON form of the
///     method result (<c>runtime.operations</c>, field <c>result</c>): camelCase members, enums as names.
/// </summary>
public static class CkOperationResultJson
{
    /// <summary>
    ///     Web defaults (case-insensitive camelCase members) and enums as strings.
    /// </summary>
    public static JsonSerializerOptions DefaultOptions { get; } = CreateOptions();

    /// <summary>
    ///     The default options plus <paramref name="converter" /> (e.g. <see cref="CkMethodTimeSpanSecondsConverter" />
    ///     for a <c>TimeSpan</c> result).
    /// </summary>
    /// <param name="converter">The additional converter</param>
    public static JsonSerializerOptions WithConverter(JsonConverter converter)
    {
        ArgumentNullException.ThrowIfNull(converter);
        var options = CreateOptions();
        options.Converters.Add(converter);
        return options;
    }

    private static JsonSerializerOptions CreateOptions() => new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}

/// <summary>
///     The common members of the generated response type of an asynchronous method field (AB#8446):
///     <c>{ invocationId operation { operationId acceptedAtUtc retryAfterSeconds expiresAtUtc } }</c>.
/// </summary>
public abstract class CkMethodAsyncCallResponse : CkMethodCallResponse
{
    /// <summary>
    ///     The operation handle as the asset repository answers it.
    /// </summary>
    public CkOperationHandleResponse? Operation { get; set; }
}

/// <summary>
///     The wire form of the operation handle (<c>CkMethodOperationHandle</c> in the asset repository's schema).
/// </summary>
public sealed class CkOperationHandleResponse
{
    /// <summary>The opaque id of the operation.</summary>
    public string? OperationId { get; set; }

    /// <summary>The time (UTC) the operation was accepted.</summary>
    public DateTime? AcceptedAtUtc { get; set; }

    /// <summary>The suggested delay before the first status check, in seconds.</summary>
    public int? RetryAfterSeconds { get; set; }

    /// <summary>The time (UTC) after which the operation may be forgotten; <c>null</c> while it is open.</summary>
    public DateTime? ExpiresAtUtc { get; set; }
}
