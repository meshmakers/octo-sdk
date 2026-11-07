namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants.Methods;

/// <summary>
///     The outcome of a CK method call through a generated method client (AB#8423): either a success with the
///     invocation id, or one of the errors the method <b>declares</b> in its CK model. Every other failure
///     (authorization, validation, timeout, transport) is thrown, see <see cref="CkMethodCallException" />.
/// </summary>
public class CkMethodResult
{
    /// <summary>
    ///     Creates a result.
    /// </summary>
    /// <param name="invocationId">The invocation id of a success</param>
    /// <param name="error">The declared error of a failure</param>
    protected CkMethodResult(string? invocationId, CkMethodError? error)
    {
        InvocationId = invocationId;
        Error = error;
    }

    /// <summary>
    ///     <c>true</c> when the method ran successfully.
    /// </summary>
    public bool IsSuccess => Error == null;

    /// <summary>
    ///     The invocation id of a successful call, for correlation with the handler log; <c>null</c> for a failure.
    /// </summary>
    public string? InvocationId { get; }

    /// <summary>
    ///     The declared error of a failed call; <c>null</c> for a success.
    /// </summary>
    public CkMethodError? Error { get; }

    /// <summary>
    ///     <c>true</c> when the call failed with the declared error <paramref name="code" />.
    /// </summary>
    /// <param name="code">A declared error code, e.g. a constant of the generated <c>…Errors</c> class</param>
    public bool IsError(string code) => string.Equals(Error?.Code, code, StringComparison.Ordinal);

    /// <summary>
    ///     A successful call of a method without result.
    /// </summary>
    /// <param name="invocationId">The invocation id</param>
    public static CkMethodResult Success(string? invocationId) => new(invocationId, null);

    /// <summary>
    ///     A call that failed with a declared error.
    /// </summary>
    /// <param name="error">The declared error</param>
    public static CkMethodResult Failure(CkMethodError error) =>
        new(null, error ?? throw new ArgumentNullException(nameof(error)));
}

/// <summary>
///     The outcome of a CK method call with a declared result (AB#8423), see <see cref="CkMethodResult" />.
/// </summary>
/// <typeparam name="TValue">The C# type of the declared result</typeparam>
public sealed class CkMethodResult<TValue> : CkMethodResult
{
    private CkMethodResult(string? invocationId, TValue? value, CkMethodError? error) : base(invocationId, error)
    {
        Value = value;
    }

    /// <summary>
    ///     The declared result of a successful call; <c>default</c> for a failure and for an accepted asynchronous call
    ///     whose result is not available yet.
    /// </summary>
    public TValue? Value { get; }

    /// <summary>
    ///     A successful call.
    /// </summary>
    /// <param name="invocationId">The invocation id</param>
    /// <param name="value">The declared result</param>
    public static CkMethodResult<TValue> Success(string? invocationId, TValue? value) => new(invocationId, value, null);

    /// <summary>
    ///     A call that failed with a declared error.
    /// </summary>
    /// <param name="error">The declared error</param>
    public new static CkMethodResult<TValue> Failure(CkMethodError error) =>
        new(null, default, error ?? throw new ArgumentNullException(nameof(error)));
}
