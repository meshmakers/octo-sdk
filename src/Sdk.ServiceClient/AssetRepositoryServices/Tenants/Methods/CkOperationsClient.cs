using System.Text.Json;
using System.Text.Json.Serialization;
using GraphQL;

namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants.Methods;

/// <summary>
///     Reads and waits for the operations of asynchronous CK methods (AB#8446) through the asset repository's
///     <c>runtime.operations</c> query (AB#8444). A generated method client of an asynchronous method returns a
///     <see cref="CkOperationHandle" />; this client turns the handle into the result:
///     <list type="bullet">
///         <item><see cref="GetOperationAsync" /> reads one operation (state, progress, result or error),</item>
///         <item><see cref="ListOperationsAsync" /> lists the caller's operations (or every caller's, for tenant administrators),</item>
///         <item><see cref="WaitForOperationAsync{TResult}" /> polls until the operation is final and returns the typed result.</item>
///     </list>
///     The client is separate from the generated method clients so that a CK method name can never collide with these
///     members. Operations cannot be cancelled (decision AB#6927): the cancellation token of a wait only ends the wait.
/// </summary>
public class CkOperationsClient
{
    /// <summary>The <c>extensions.code</c> of an unknown, expired or foreign operation id.</summary>
    public const string OperationNotFound = "OPERATION_NOT_FOUND";

    /// <summary>The <c>extensions.code</c> of a list query the caller may not run (<c>allOwners</c> without administrator rights).</summary>
    public const string OperationQueryRefused = "OPERATION_QUERY_REFUSED";

    /// <summary>
    ///     <see cref="CkMethodError.Message" /> of an operation error: the operation carries no message text (code and
    ///     parameters only, language-neutral).
    /// </summary>
    public const string OperationFailedMessage = "The operation failed.";

    /// <summary>The selection of one operation.</summary>
    internal const string OperationSelection =
        "operationId methodId invocationId state progress { percent stageCode parameters { name value } } result " +
        "error { code parameters { name value } details { code parameters { name value } } } " +
        "acceptedAtUtc startedAtUtc completedAtUtc expiresAtUtc";

    /// <summary>The document of <see cref="GetOperationAsync" />.</summary>
    public const string GetOperationDocument =
        "query CkOperationById($operationId: ID!) { runtime { operations(operationId: $operationId) { totalCount items { " +
        OperationSelection + " } } } }";

    /// <summary>The document of <see cref="ListOperationsAsync" />.</summary>
    public const string ListOperationsDocument =
        "query CkOperationList($methodId: String, $state: CkMethodOperationState, $since: DateTime, $allOwners: Boolean, $skip: Int, $take: Int) " +
        "{ runtime { operations(methodId: $methodId, state: $state, since: $since, allOwners: $allOwners, skip: $skip, take: $take) { totalCount items { " +
        OperationSelection + " } } } }";

    /// <summary>
    ///     Creates the client.
    /// </summary>
    /// <param name="tenantClient">The tenant client of the tenant the operations belong to</param>
    public CkOperationsClient(ITenantClient tenantClient)
    {
        TenantClient = tenantClient ?? throw new ArgumentNullException(nameof(tenantClient));
    }

    /// <summary>
    ///     The tenant client the queries go through.
    /// </summary>
    protected ITenantClient TenantClient { get; }

    /// <summary>
    ///     Reads one operation. Readable by the caller who started it and by tenant administrators.
    /// </summary>
    /// <param name="operationId">The operation id (<see cref="CkOperationHandle.OperationId" />)</param>
    /// <param name="cancellationToken">Cancels the request</param>
    /// <returns>The operation</returns>
    /// <exception cref="CkOperationQueryException">
    ///     <c>OPERATION_NOT_FOUND</c>: the id is unknown, expired (retention) or belongs to another caller.
    /// </exception>
    public async Task<CkOperation> GetOperationAsync(string operationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        cancellationToken.ThrowIfCancellationRequested();

        var page = await QueryAsync(new GraphQLRequest(GetOperationDocument, new { operationId }, "CkOperationById"),
            cancellationToken);
        return page.Items.FirstOrDefault() ??
               throw new CkOperationQueryException(
                   new CkMethodError(OperationNotFound, "The operation was not found.",
                       new Dictionary<string, string>(), []), null, null);
    }

    /// <summary>
    ///     Lists operations, newest accepted first: by default the caller's own operations.
    /// </summary>
    /// <param name="filter">Method, state, time and paging filter; <c>null</c> for the first page of all own operations</param>
    /// <param name="cancellationToken">Cancels the request</param>
    /// <returns>The page and the total count</returns>
    /// <exception cref="CkOperationQueryException"><c>OPERATION_QUERY_REFUSED</c> for <see cref="CkOperationListFilter.AllOwners" /> without administrator rights.</exception>
    public Task<CkOperationPage> ListOperationsAsync(CkOperationListFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        filter ??= new CkOperationListFilter();
        if (filter.State == CkOperationState.Unknown)
        {
            throw new ArgumentException("Unknown is not a filter state.", nameof(filter));
        }

        var variables = new ListVariables
        {
            MethodId = string.IsNullOrWhiteSpace(filter.MethodId) ? null : filter.MethodId.Trim(),
            State = filter.State == null ? null : ToWire(filter.State.Value),
            Since = filter.Since == null ? null : AsUtc(filter.Since.Value),
            AllOwners = filter.AllOwners,
            Skip = filter.Skip,
            Take = filter.Take
        };
        return QueryAsync(new GraphQLRequest(ListOperationsDocument, variables, "CkOperationList"), cancellationToken);
    }

    /// <summary>
    ///     Waits until the operation of <paramref name="handle" /> is final and returns its result: the typed value on
    ///     <c>SUCCEEDED</c>; a declared error of the method as <see cref="CkMethodResult.Error" /> on <c>FAILED</c>.
    /// </summary>
    /// <param name="handle">The handle the generated method client returned</param>
    /// <param name="progress">Called with the operation at the start and whenever its state or progress changes</param>
    /// <param name="options">Poll interval and hooks; <c>null</c> for the defaults</param>
    /// <param name="cancellationToken">Ends the wait (throws <see cref="OperationCanceledException" />); the operation itself keeps running</param>
    /// <typeparam name="TResult">The C# type of the declared result</typeparam>
    /// <returns>The result of the operation</returns>
    /// <exception cref="CkMethodCallException">
    ///     The operation failed with a code the method does not declare, e.g. <c>METHOD_OUTCOME_UNKNOWN</c> (<c>reason</c>
    ///     <c>handler-lost</c>: the handler stopped, the operation is not started again) or <c>METHOD_HANDLER_FAILED</c>.
    /// </exception>
    /// <exception cref="CkOperationQueryException"><c>OPERATION_NOT_FOUND</c>: the operation expired or is not readable by the caller.</exception>
    public async Task<CkMethodResult<TResult>> WaitForOperationAsync<TResult>(CkOperationHandle<TResult> handle,
        IProgress<CkOperation>? progress = null, CkOperationWaitOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handle);
        var operation = await WaitForCompletionAsync(handle, progress, options, cancellationToken);
        var error = DeclaredErrorOrThrow(handle, operation);
        return error != null
            ? CkMethodResult<TResult>.Failure(error)
            : CkMethodResult<TResult>.Success(operation.InvocationId, handle.ReadResult(operation));
    }

    /// <summary>
    ///     Waits until the operation of <paramref name="handle" /> (a method without result) is final, see
    ///     <see cref="WaitForOperationAsync{TResult}" />.
    /// </summary>
    /// <param name="handle">The handle the generated method client returned</param>
    /// <param name="progress">Called with the operation at the start and whenever its state or progress changes</param>
    /// <param name="options">Poll interval and hooks; <c>null</c> for the defaults</param>
    /// <param name="cancellationToken">Ends the wait; the operation itself keeps running</param>
    /// <returns>Success, or the declared error of the method</returns>
    /// <exception cref="CkMethodCallException">The operation failed with a code the method does not declare.</exception>
    /// <exception cref="CkOperationQueryException"><c>OPERATION_NOT_FOUND</c>.</exception>
    public async Task<CkMethodResult> WaitForOperationAsync(CkOperationHandle handle,
        IProgress<CkOperation>? progress = null, CkOperationWaitOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handle);
        var operation = await WaitForCompletionAsync(handle, progress, options, cancellationToken);
        var error = DeclaredErrorOrThrow(handle, operation);
        return error != null ? CkMethodResult.Failure(error) : CkMethodResult.Success(operation.InvocationId);
    }

    /// <summary>
    ///     Polls the operation until it is final and returns it as read, without judging the error (the building block of
    ///     <see cref="WaitForOperationAsync{TResult}" />; also for a caller that only knows the operation id). Between two
    ///     reads it waits <see cref="CkOperationHandle.RetryAfterSeconds" /> (at least
    ///     <see cref="CkOperationWaitOptions.MinPollInterval" />, at most <see cref="CkOperationWaitOptions.MaxPollInterval" />).
    /// </summary>
    /// <param name="handle">The handle; <c>new CkOperationHandle(operationId)</c> for a stored id</param>
    /// <param name="progress">Called with the operation at the start and whenever its state or progress changes</param>
    /// <param name="options">Poll interval and hooks; <c>null</c> for the defaults</param>
    /// <param name="cancellationToken">Ends the wait; the operation itself keeps running</param>
    /// <returns>The final operation (<c>SUCCEEDED</c> or <c>FAILED</c>)</returns>
    /// <exception cref="CkOperationQueryException"><c>OPERATION_NOT_FOUND</c>.</exception>
    public async Task<CkOperation> WaitForCompletionAsync(CkOperationHandle handle,
        IProgress<CkOperation>? progress = null, CkOperationWaitOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handle);
        options ??= new CkOperationWaitOptions();
        var interval = PollInterval(handle.RetryAfterSeconds, options);
        CkOperation? last = null;
        var first = true;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!first || options.WaitBeforeFirstRead)
            {
                await Task.Delay(interval, options.TimeProvider, cancellationToken);
            }

            first = false;
            if (options.BeforeReadAsync != null)
            {
                await options.BeforeReadAsync(cancellationToken);
            }

            var operation = await GetOperationAsync(handle.OperationId, cancellationToken);
            if (last == null || HasChanged(last, operation))
            {
                progress?.Report(operation);
            }

            if (operation.IsFinal)
            {
                return operation;
            }

            last = operation;
        }
    }

    /// <summary>
    ///     The delay between two reads: <paramref name="retryAfterSeconds" /> within the bounds of the options,
    ///     <see cref="CkOperationWaitOptions.DefaultPollInterval" /> when the service suggested none.
    /// </summary>
    internal static TimeSpan PollInterval(int? retryAfterSeconds, CkOperationWaitOptions options)
    {
        var interval = retryAfterSeconds is > 0
            ? TimeSpan.FromSeconds(retryAfterSeconds.Value)
            : options.DefaultPollInterval;
        if (interval < options.MinPollInterval)
        {
            interval = options.MinPollInterval;
        }

        if (interval > options.MaxPollInterval)
        {
            interval = options.MaxPollInterval;
        }

        return interval;
    }

    /// <summary>An unspecified kind is UTC (the SDK convention, as for <c>RawPointDto</c>); local times are converted.</summary>
    internal static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => value
    };

    private static bool HasChanged(CkOperation previous, CkOperation current) =>
        previous.State != current.State ||
        previous.Progress?.Percent != current.Progress?.Percent ||
        !string.Equals(previous.Progress?.StageCode, current.Progress?.StageCode, StringComparison.Ordinal) ||
        !SameParameters(previous.Progress?.Parameters, current.Progress?.Parameters);

    private static bool SameParameters(IReadOnlyDictionary<string, string>? a, IReadOnlyDictionary<string, string>? b)
    {
        if (a == null || b == null)
        {
            return a == null && b == null;
        }

        return a.Count == b.Count && a.All(p => b.TryGetValue(p.Key, out var v) && string.Equals(v, p.Value, StringComparison.Ordinal));
    }

    /// <summary>
    ///     <c>null</c> for a succeeded operation, the error for a declared code; throws for every other failure.
    /// </summary>
    private static CkMethodError? DeclaredErrorOrThrow(CkOperationHandle handle, CkOperation operation)
    {
        if (operation.State == CkOperationState.Succeeded)
        {
            return null;
        }

        var error = operation.Error ?? new CkMethodError("METHOD_HANDLER_FAILED",
            "The operation failed without an error.", new Dictionary<string, string>(), []);
        if (handle.DeclaredErrorCodes.Contains(error.Code, StringComparer.Ordinal))
        {
            return error;
        }

        throw new CkMethodCallException(handle.MethodId ?? operation.MethodId, error, null, null);
    }

    private async Task<CkOperationPage> QueryAsync(GraphQLRequest request, CancellationToken cancellationToken)
    {
        QlItemsContainer<OperationWire>? container;
        try
        {
            container = await TenantClient.SendQueryAsync<OperationWire>(request).WaitAsync(cancellationToken);
        }
        catch (QlQueryErrorException e) when (e is not CkOperationQueryException)
        {
            var error = CkMethodErrorReader.Read(e.Errors);
            if (error != null && error.Code.StartsWith("OPERATION_", StringComparison.Ordinal))
            {
                throw new CkOperationQueryException(error, e.Errors, e);
            }

            throw;
        }

        var items = container?.Items?.Select(ToOperation).ToList() ?? [];
        return new CkOperationPage(container?.TotalCount ?? items.Count, items);
    }

    private static CkOperation ToOperation(OperationWire wire)
    {
        var stateName = wire.State ?? string.Empty;
        return new CkOperation(
            wire.OperationId ?? string.Empty,
            wire.MethodId ?? string.Empty,
            wire.InvocationId,
            FromWire(stateName),
            stateName,
            wire.Progress == null
                ? null
                : new CkOperationProgress(wire.Progress.Percent, wire.Progress.StageCode, ToDictionary(wire.Progress.Parameters)),
            wire.Result is { ValueKind: not JsonValueKind.Null and not JsonValueKind.Undefined } result ? result.Clone() : null,
            wire.Error == null || string.IsNullOrWhiteSpace(wire.Error.Code)
                ? null
                : new CkMethodError(wire.Error.Code, OperationFailedMessage, ToDictionary(wire.Error.Parameters),
                    (wire.Error.Details ?? [])
                    .Where(d => !string.IsNullOrWhiteSpace(d.Code))
                    .Select(d => new CkMethodErrorDetail(d.Code!, ToDictionary(d.Parameters)))
                    .ToList()),
            wire.AcceptedAtUtc,
            wire.StartedAtUtc,
            wire.CompletedAtUtc,
            wire.ExpiresAtUtc);
    }

    private static IReadOnlyDictionary<string, string> ToDictionary(List<ParameterWire>? parameters)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var parameter in parameters ?? [])
        {
            if (!string.IsNullOrEmpty(parameter.Name))
            {
                result[parameter.Name] = parameter.Value ?? string.Empty;
            }
        }

        return result;
    }

    internal static CkOperationState FromWire(string state) => state switch
    {
        "ACCEPTED" => CkOperationState.Accepted,
        "RUNNING" => CkOperationState.Running,
        "SUCCEEDED" => CkOperationState.Succeeded,
        "FAILED" => CkOperationState.Failed,
        _ => CkOperationState.Unknown
    };

    internal static string ToWire(CkOperationState state) => state switch
    {
        CkOperationState.Accepted => "ACCEPTED",
        CkOperationState.Running => "RUNNING",
        CkOperationState.Succeeded => "SUCCEEDED",
        CkOperationState.Failed => "FAILED",
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
    };

    private sealed class ListVariables
    {
        [JsonPropertyName("methodId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MethodId { get; init; }

        [JsonPropertyName("state")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? State { get; init; }

        [JsonPropertyName("since")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? Since { get; init; }

        [JsonPropertyName("allOwners")]
        public bool AllOwners { get; init; }

        [JsonPropertyName("skip")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Skip { get; init; }

        [JsonPropertyName("take")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Take { get; init; }
    }

    private sealed class OperationWire
    {
        [JsonPropertyName("operationId")] public string? OperationId { get; set; }
        [JsonPropertyName("methodId")] public string? MethodId { get; set; }
        [JsonPropertyName("invocationId")] public string? InvocationId { get; set; }
        [JsonPropertyName("state")] public string? State { get; set; }
        [JsonPropertyName("progress")] public ProgressWire? Progress { get; set; }
        [JsonPropertyName("result")] public JsonElement? Result { get; set; }
        [JsonPropertyName("error")] public ErrorWire? Error { get; set; }
        [JsonPropertyName("acceptedAtUtc")] public DateTime? AcceptedAtUtc { get; set; }
        [JsonPropertyName("startedAtUtc")] public DateTime? StartedAtUtc { get; set; }
        [JsonPropertyName("completedAtUtc")] public DateTime? CompletedAtUtc { get; set; }
        [JsonPropertyName("expiresAtUtc")] public DateTime? ExpiresAtUtc { get; set; }
    }

    private sealed class ProgressWire
    {
        [JsonPropertyName("percent")] public int? Percent { get; set; }
        [JsonPropertyName("stageCode")] public string? StageCode { get; set; }
        [JsonPropertyName("parameters")] public List<ParameterWire>? Parameters { get; set; }
    }

    private sealed class ErrorWire
    {
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("parameters")] public List<ParameterWire>? Parameters { get; set; }
        [JsonPropertyName("details")] public List<DetailWire>? Details { get; set; }
    }

    private sealed class DetailWire
    {
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("parameters")] public List<ParameterWire>? Parameters { get; set; }
    }

    private sealed class ParameterWire
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("value")] public string? Value { get; set; }
    }
}

/// <summary>
///     Options of a wait for an operation (AB#8446).
/// </summary>
public sealed class CkOperationWaitOptions
{
    /// <summary>The delay between two reads when the handle carries no <see cref="CkOperationHandle.RetryAfterSeconds" />; default 2 s.</summary>
    public TimeSpan DefaultPollInterval { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>The shortest delay between two reads; default 1 s.</summary>
    public TimeSpan MinPollInterval { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>The longest delay between two reads, also when the service suggests more; default 30 s.</summary>
    public TimeSpan MaxPollInterval { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     <c>true</c> (default) waits <see cref="CkOperationHandle.RetryAfterSeconds" /> before the first read, as the
    ///     service suggests; <c>false</c> reads at once (e.g. for a handle created from a stored id).
    /// </summary>
    public bool WaitBeforeFirstRead { get; init; } = true;

    /// <summary>
    ///     Called before every read, e.g. to refresh the access token of the tenant client during a long wait.
    /// </summary>
    public Func<CancellationToken, Task>? BeforeReadAsync { get; init; }

    /// <summary>The time provider of the delays (tests); default <see cref="TimeProvider.System" />.</summary>
    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;
}

/// <summary>
///     A <c>runtime.operations</c> query was refused (AB#8446): <c>OPERATION_NOT_FOUND</c> (unknown, expired or foreign
///     id; all three look the same) or <c>OPERATION_QUERY_REFUSED</c>. It is a <see cref="QlQueryErrorException" />.
/// </summary>
[Serializable]
public class CkOperationQueryException : QlQueryErrorException
{
    /// <summary>
    ///     Creates the exception.
    /// </summary>
    /// <param name="error">The error the service answered</param>
    /// <param name="errors">All GraphQL errors of the response</param>
    /// <param name="inner">The exception the GraphQL client threw</param>
    public CkOperationQueryException(CkMethodError error, IReadOnlyList<GraphQLError>? errors, Exception? inner)
        : base($"The operation query failed: {error.Code}", errors, inner)
    {
        Error = error;
    }

    /// <summary>The error code and parameters the service answered.</summary>
    public CkMethodError Error { get; }

    /// <summary>Shortcut for <see cref="CkMethodError.Code" /> of <see cref="Error" />.</summary>
    public string Code => Error.Code;
}
