using System.Reflection;
using System.Text.Json;
using GraphQL;
using GraphQL.Types;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants.Methods;

namespace Sdk.SourceGeneration.Tests;

/// <summary>
///     AB#8446 (F3.6): the generated client of an <b>asynchronous</b> CK method returns a <see cref="CkOperationHandle" />,
///     and <see cref="CkOperationsClient" /> reads, lists and waits for the operation. Generator input is the System.Bot
///     3.7.0 model (<c>TenantRepository.Dump-1</c>, <c>execution.mode: async</c>, record result, declared errors
///     <c>DUMP_FAILED</c> / <c>DUMP_INTERRUPTED</c>) compiled from octo-bot-services; schema and answers were recorded
///     from the asset repository on test-2 (a scratch tenant with System.Bot 3.7.0, a real dump), see
///     <c>docs/method-clients.md</c>.
/// </summary>
public class AsyncMethodClientTests : IClassFixture<MethodClientRuntimeTests.LoopbackGraphQlService>
{
    private const string Ns = "Consumer.DataTransferObjects.System.Bot.v3";
    private const string ClientHint = $"{Ns}.Type.TenantRepositoryMethodClient.g.cs";
    private const string Methods = "global::Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants.Methods";

    private const string ExpectedDumpDocument =
        "mutation SystemBotTenantRepositoryDump($input: SystemBotTenantRepositoryDumpMethodInput, $idempotencyKey: String) " +
        "{ runtime { systemBotTenantRepositorys { dump(input: $input, idempotencyKey: $idempotencyKey) " +
        "{ invocationId operation { operationId acceptedAtUtc retryAfterSeconds expiresAtUtc } } } } }";

    private static readonly Lazy<SortedDictionary<string, string>> Sources =
        new(() => GeneratorFixtures.Emit(GeneratorFixtures.SystemBot, "Consumer"));

    private static readonly Lazy<Assembly> Generated = new(() => GeneratorFixtures.CompileAndLoad(Sources.Value.Values));

    private static readonly Lazy<ISchema> Schema =
        new(() => MethodClientSchemaValidationTests.LoadSchema(GeneratorFixtures.SystemBotSchemaFile));

    private readonly MethodClientRuntimeTests.LoopbackGraphQlService _service;

    public AsyncMethodClientTests(MethodClientRuntimeTests.LoopbackGraphQlService service)
    {
        _service = service;
        _service.Reset();
    }

    [Fact]
    public void DumpClient_ReturnsAnOperationHandleWithTheTypedResult()
    {
        var code = Sources.Value[ClientHint];

        Assert.Contains(
            $"public global::System.Threading.Tasks.Task<{Methods}.CkMethodResult<{Methods}.CkOperationHandle<global::{Ns}.RtRepositoryDumpResultRecordDto?>>> DumpAsync(bool? includeArchiveData = null, string? idempotencyKey = null)",
            code);
        Assert.Contains($"private sealed class DumpResponse : {Methods}.CkMethodAsyncCallResponse", code);
        Assert.Contains("Calls the asynchronous CK method <c>System.Bot/TenantRepository.Dump-1</c>", code);
    }

    [Fact]
    public void DumpDocument_SelectsTheOperationHandle_NotTheResult()
    {
        Assert.Equal(ExpectedDumpDocument, Constant("DumpDocument"));
    }

    [Fact]
    public void GeneratedSources_CompileWithoutWarnings()
    {
        Assert.NotNull(Generated.Value.GetType($"{Ns}.RtTenantRepositoryMethodClient"));
        Assert.Equal(["DUMP_FAILED", "DUMP_INTERRUPTED"],
            Generated.Value.GetType($"{Ns}.RtTenantRepositoryMethodClient+DumpErrors")!.GetFields()
                .Select(f => (string)f.GetValue(null)!));
    }

    [Fact]
    public async Task DumpDocument_ValidatesAgainstTheRecordedSchemaOfATenantWithSystemBot37()
    {
        var errors = await MethodClientSchemaValidationTests.DocumentErrorsAsync(Schema.Value, Constant("DumpDocument"));

        Assert.True(errors.Count == 0, string.Join("; ", errors));
    }

    [Theory]
    [InlineData(CkOperationsClient.GetOperationDocument)]
    [InlineData(CkOperationsClient.ListOperationsDocument)]
    public async Task OperationDocuments_ValidateAgainstTheRecordedSchema(string document)
    {
        var errors = await MethodClientSchemaValidationTests.DocumentErrorsAsync(Schema.Value, document);

        Assert.True(errors.Count == 0, string.Join("; ", errors));
    }

    [Fact]
    public async Task BrokenAsyncDocument_SelectingTheResult_IsRejected_SoTheGateIsReal()
    {
        // What the generator sent before AB#8446: an asynchronous method has no 'result' field.
        const string before =
            "mutation M($input: SystemBotTenantRepositoryDumpMethodInput, $idempotencyKey: String) { runtime { systemBotTenantRepositorys { dump(input: $input, idempotencyKey: $idempotencyKey) { invocationId result { downloadPath jobId } } } } }";

        Assert.NotEmpty(await MethodClientSchemaValidationTests.DocumentErrorsAsync(Schema.Value, before));
    }

    [Fact]
    public async Task RecordedAcceptedAnswer_BecomesTheHandle()
    {
        _service.Respond(200, Recorded("dump-accepted"));

        var handle = await StartDumpAsync(includeArchiveData: false);

        Assert.Equal("cff5f253aebe408c9b170bcf4922975b", handle.OperationId);
        Assert.Equal("System.Bot/TenantRepository.Dump-1", handle.MethodId);
        Assert.Equal("ebe985ef-fa78-4ece-a870-977335231e87", handle.InvocationId);
        Assert.Equal(5, handle.RetryAfterSeconds);
        Assert.Equal(DateTimeOffset.Parse("2026-11-10T21:29:38.7680019Z", System.Globalization.CultureInfo.InvariantCulture).UtcDateTime,
            handle.AcceptedAtUtc!.Value.ToUniversalTime());
        Assert.Null(handle.ExpiresAtUtc);
        Assert.Equal(["DUMP_FAILED", "DUMP_INTERRUPTED"], handle.DeclaredErrorCodes);
        var body = Assert.Single(_service.Bodies);
        Assert.Contains("\"input\":{\"includeArchiveData\":false}", body, StringComparison.Ordinal);
        Assert.Contains("\"operationName\":\"SystemBotTenantRepositoryDump\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeclaredErrorAtAccept_IsAResult_WithoutAHandle()
    {
        // A handler validates synchronously and may refuse with a declared error before it accepts (method-handler-sdk §11).
        _service.Respond(200, """
            {"errors":[{"message":"The CK method call failed: DUMP_FAILED","extensions":{"code":"DUMP_FAILED","parameters":{"reason":"another-dump-running"}}}],
             "data":{"runtime":{"systemBotTenantRepositorys":{"dump":null}}}}
            """);

        var result = await InvokeDumpAsync(null);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsError("DUMP_FAILED"));
        Assert.Null(result.GetType().GetProperty("Value")!.GetValue(result));
    }

    [Fact]
    public async Task AcceptedAnswerWithoutHandle_Throws()
    {
        _service.Respond(200, """{"data":{"runtime":{"systemBotTenantRepositorys":{"dump":{"invocationId":"i-1","operation":null}}}}}""");

        var e = await Assert.ThrowsAsync<Meshmakers.Octo.Sdk.ServiceClient.ServiceClientException>(() => InvokeDumpAsync(null));

        Assert.Contains("returned no operation handle", e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WaitForOperation_PollsUntilSucceeded_ReportsProgress_AndReturnsTheTypedResult()
    {
        _service.Respond(200, Recorded("dump-accepted"));
        var handle = await StartDumpAsync(includeArchiveData: null);
        _service.Reset();
        _service.RespondInOrder(Recorded("operation-running"), Recorded("operation-running"),
            Recorded("operation-succeeded"));
        var time = new RecordingTimeProvider();
        var reports = new List<CkOperation>();

        var result = await WaitAsync(handle, new SyncProgress(reports.Add), new CkOperationWaitOptions { TimeProvider = time }, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("ebe985ef-fa78-4ece-a870-977335231e87", result.InvocationId);
        var value = result.GetType().GetProperty("Value")!.GetValue(result)!;
        Assert.Equal("6af38cc34f052289b00e91d6", value.GetType().GetProperty("JobId")!.GetValue(value));
        Assert.Equal("tmp-d8446-ops/v1/jobs/download?id=6af38cc34f052289b00e91d6",
            value.GetType().GetProperty("DownloadPath")!.GetValue(value));
        // retryAfterSeconds = 5 is the delay before every read
        Assert.Equal([TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5)], time.Delays);
        // the unchanged second RUNNING read is not reported again
        Assert.Equal([CkOperationState.Running, CkOperationState.Succeeded], reports.Select(r => r.State));
        Assert.Equal("QUEUED", reports[0].Progress!.StageCode);
        Assert.Equal("6af38cc34f052289b00e91d6", reports[0].Progress!.Parameters["jobId"]);
        Assert.Equal(80, reports[1].Progress!.Percent);
        Assert.All(_service.Bodies, b => Assert.Contains("\"operationName\":\"CkOperationById\"", b, StringComparison.Ordinal));
        Assert.All(_service.Bodies, b => Assert.Contains("\"operationId\":\"cff5f253aebe408c9b170bcf4922975b\"", b, StringComparison.Ordinal));
    }

    [Fact]
    public async Task WaitForOperation_DeclaredError_IsAResult()
    {
        _service.RespondInOrder(Failed("DUMP_FAILED", ("jobId", "j-1"), ("reason", "another-dump-running")));
        var handle = Handle(retryAfterSeconds: 1);

        var result = await WaitAsync(handle, null, new CkOperationWaitOptions { TimeProvider = new RecordingTimeProvider() }, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsError("DUMP_FAILED"));
        Assert.Equal("another-dump-running", result.Error!.Parameters["reason"]);
        Assert.Equal("j-1", result.Error.Parameters["jobId"]);
    }

    [Fact]
    public async Task WaitForOperation_HandlerLost_IsThrownAsMethodOutcomeUnknown()
    {
        _service.RespondInOrder(Failed("METHOD_OUTCOME_UNKNOWN", ("reason", "handler-lost")));
        var handle = Handle(retryAfterSeconds: 1);

        var e = await Assert.ThrowsAsync<CkMethodCallException>(() =>
            WaitAsync(handle, null, new CkOperationWaitOptions { TimeProvider = new RecordingTimeProvider() }, TestContext.Current.CancellationToken));

        Assert.Equal("METHOD_OUTCOME_UNKNOWN", e.Code);
        Assert.Equal("handler-lost", e.Error.Parameters["reason"]);
        Assert.Equal("System.Bot/TenantRepository.Dump-1", e.MethodId);
    }

    [Fact]
    public async Task WaitForOperation_Cancellation_EndsOnlyTheWait()
    {
        _service.Respond(200, Recorded("operation-running"));
        var handle = Handle(retryAfterSeconds: 1);
        using var cancellation = new CancellationTokenSource();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => WaitAsync(handle,
            new SyncProgress(_ => cancellation.Cancel()),
            new CkOperationWaitOptions { TimeProvider = new RecordingTimeProvider() }, cancellation.Token));

        // One read, then the wait ended: nothing else was sent - in particular no mutation (operations have no cancel).
        var body = Assert.Single(_service.Bodies);
        Assert.Contains("query CkOperationById", body, StringComparison.Ordinal);
        Assert.DoesNotContain("mutation", body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null, 2)]
    [InlineData(0, 2)]
    [InlineData(5, 5)]
    [InlineData(600, 30)]
    public async Task WaitForOperation_HonoursRetryAfterSeconds_WithinTheBounds(int? retryAfterSeconds, int expectedSeconds)
    {
        _service.RespondInOrder(Recorded("operation-succeeded"));
        var time = new RecordingTimeProvider();

        await WaitAsync(Handle(retryAfterSeconds), null, new CkOperationWaitOptions { TimeProvider = time }, TestContext.Current.CancellationToken);

        Assert.Equal([TimeSpan.FromSeconds(expectedSeconds)], time.Delays);
    }

    [Fact]
    public async Task WaitForCompletion_FromAStoredId_ReadsAtOnce_AndCallsTheHookBeforeEveryRead()
    {
        _service.RespondInOrder(Recorded("operation-running"), Recorded("operation-succeeded"));
        var time = new RecordingTimeProvider();
        var hookCalls = 0;

        var operation = await new CkOperationsClient(RealTenantClient()).WaitForCompletionAsync(
            new CkOperationHandle("cff5f253aebe408c9b170bcf4922975b"), null,
            new CkOperationWaitOptions
            {
                TimeProvider = time, WaitBeforeFirstRead = false,
                BeforeReadAsync = _ => { hookCalls++; return Task.CompletedTask; }
            }, TestContext.Current.CancellationToken);

        Assert.Equal(CkOperationState.Succeeded, operation.State);
        Assert.True(operation.IsFinal);
        Assert.Equal("6af38cc34f052289b00e91d6", operation.Result!.Value.GetProperty("jobId").GetString());
        Assert.Equal([TimeSpan.FromSeconds(2)], time.Delays);
        Assert.Equal(2, hookCalls);
    }

    [Fact]
    public async Task GetOperation_RecordedNotFound_ThrowsOperationNotFound()
    {
        _service.Respond(200, Recorded("operation-not-found"));

        var e = await Assert.ThrowsAsync<CkOperationQueryException>(() =>
            new CkOperationsClient(RealTenantClient()).GetOperationAsync("00000000000000000000000000000000", TestContext.Current.CancellationToken));

        Assert.Equal(CkOperationsClient.OperationNotFound, e.Code);
    }

    [Fact]
    public async Task ListOperations_SendsTheFilter_AndReadsTheRecordedPage()
    {
        _service.Respond(200, Recorded("operation-list"));

        var page = await new CkOperationsClient(RealTenantClient()).ListOperationsAsync(new CkOperationListFilter
        {
            MethodId = "System.Bot/TenantRepository.Dump-1", State = CkOperationState.Succeeded, Take = 10
        }, TestContext.Current.CancellationToken);

        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.Items.Count);
        Assert.All(page.Items, o => Assert.Equal(CkOperationState.Succeeded, o.State));
        Assert.All(page.Items, o => Assert.NotNull(o.ExpiresAtUtc));
        var body = Assert.Single(_service.Bodies);
        Assert.Contains("\"methodId\":\"System.Bot/TenantRepository.Dump-1\"", body, StringComparison.Ordinal);
        Assert.Contains("\"state\":\"SUCCEEDED\"", body, StringComparison.Ordinal);
        Assert.Contains("\"take\":10", body, StringComparison.Ordinal);
        Assert.Contains("\"allOwners\":false", body, StringComparison.Ordinal);
        Assert.DoesNotContain("\"since\"", body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Utc)]
    public async Task ListOperations_SinceWithoutAKind_IsUtc(DateTimeKind kind)
    {
        _service.Respond(200, Recorded("operation-list"));

        await new CkOperationsClient(RealTenantClient()).ListOperationsAsync(
            new CkOperationListFilter { Since = new DateTime(2026, 11, 10, 8, 0, 0, kind) }, TestContext.Current.CancellationToken);

        Assert.Contains("\"since\":\"2026-11-10T08:00:00Z\"", Assert.Single(_service.Bodies), StringComparison.Ordinal);
    }

    [Fact]
    public void RuntimeWithoutOperationTypes_LeavesOutAsyncMethods_AndStillCompiles()
    {
        // A ServiceClient that has CkMethodClientBase but not CkMethodAsyncCallResponse (older package).
        var sources = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var (model, cache, tenantId) = GeneratorFixtures.Restore(GeneratorFixtures.SystemBot);
        Meshmakers.Octo.Sdk.SourceGeneration.CkModelSourceEmitter.Emit("Consumer", model, tenantId, cache,
            (hint, code) => sources.Add(hint, code), emitMethodClients: true, emitAsyncMethodClients: false);

        var code = sources[ClientHint];
        Assert.Contains(
            "// Unsupported by Generator: method System.Bot/TenantRepository.Dump-1 (asynchronous; needs Meshmakers.Octo.Sdk.ServiceClient with CkOperationHandle)",
            code);
        Assert.DoesNotContain("DumpAsync", code, StringComparison.Ordinal);
        Assert.DoesNotContain("CkMethodAsyncCallResponse", code, StringComparison.Ordinal);
        Assert.NotNull(GeneratorFixtures.CompileAndLoad(sources.Values));
    }

    private async Task<CkMethodResult> InvokeDumpAsync(bool? includeArchiveData)
    {
        var clientType = Generated.Value.GetType($"{Ns}.RtTenantRepositoryMethodClient")!;
        var client = Activator.CreateInstance(clientType, RealTenantClient())!;
        var task = (Task)clientType.GetMethod("DumpAsync")!.Invoke(client, [includeArchiveData, null])!;
        await task;
        return (CkMethodResult)task.GetType().GetProperty("Result")!.GetValue(task)!;
    }

    private async Task<CkOperationHandle> StartDumpAsync(bool? includeArchiveData)
    {
        var result = await InvokeDumpAsync(includeArchiveData);
        Assert.True(result.IsSuccess);
        return (CkOperationHandle)result.GetType().GetProperty("Value")!.GetValue(result)!;
    }

    /// <summary>A handle of the generated result type, as the client returns it, for a stored operation id.</summary>
    private static CkOperationHandle Handle(int? retryAfterSeconds)
    {
        var resultType = Generated.Value.GetType($"{Ns}.RtRepositoryDumpResultRecordDto")!;
        return (CkOperationHandle)Activator.CreateInstance(typeof(CkOperationHandle<>).MakeGenericType(resultType),
            "cff5f253aebe408c9b170bcf4922975b", "System.Bot/TenantRepository.Dump-1", null, null, retryAfterSeconds, null,
            (IReadOnlyCollection<string>)["DUMP_FAILED", "DUMP_INTERRUPTED"], null)!;
    }

    /// <summary>Calls the typed <see cref="CkOperationsClient.WaitForOperationAsync{TResult}" /> for the handle's result type.</summary>
    private async Task<CkMethodResult> WaitAsync(CkOperationHandle handle, IProgress<CkOperation>? progress,
        CkOperationWaitOptions options, CancellationToken cancellationToken = default)
    {
        var resultType = handle.GetType().GetGenericArguments()[0];
        var method = typeof(CkOperationsClient).GetMethods()
            .Single(m => m.Name == nameof(CkOperationsClient.WaitForOperationAsync) && m.IsGenericMethodDefinition)
            .MakeGenericMethod(resultType);
        var task = (Task)method.Invoke(new CkOperationsClient(RealTenantClient()),
            [handle, progress, options, cancellationToken])!;
        await task;
        return (CkMethodResult)task.GetType().GetProperty("Result")!.GetValue(task)!;
    }

    private static string Failed(string code, params (string Name, string Value)[] parameters)
    {
        var answer = JsonDocument.Parse(Recorded("operation-succeeded")).RootElement;
        var item = answer.GetProperty("data").GetProperty("runtime").GetProperty("operations").GetProperty("items")[0];
        var failed = new Dictionary<string, object?>
        {
            ["operationId"] = item.GetProperty("operationId").GetString(),
            ["methodId"] = item.GetProperty("methodId").GetString(),
            ["invocationId"] = item.GetProperty("invocationId").GetString(),
            ["state"] = "FAILED",
            ["progress"] = null,
            ["result"] = null,
            ["error"] = new
            {
                code,
                parameters = parameters.Select(p => new { name = p.Name, value = p.Value }).ToArray(),
                details = Array.Empty<object>()
            },
            ["acceptedAtUtc"] = item.GetProperty("acceptedAtUtc").GetString(),
            ["startedAtUtc"] = item.GetProperty("startedAtUtc").GetString(),
            ["completedAtUtc"] = item.GetProperty("completedAtUtc").GetString(),
            ["expiresAtUtc"] = item.GetProperty("expiresAtUtc").GetString()
        };
        return JsonSerializer.Serialize(new
        {
            data = new { runtime = new { operations = new { totalCount = 1, items = new[] { failed } } } }
        });
    }

    private static string Recorded(string name) =>
        File.ReadAllText(Path.Combine(GeneratorFixtures.FixtureDirectory, $"assetrepo-bot-answer-{name}.json"));

    private static string Constant(string name) =>
        (string)Generated.Value.GetType($"{Ns}.RtTenantRepositoryMethodClient")!.GetField(name)!.GetValue(null)!;

    private ITenantClient RealTenantClient()
    {
        var token = FakeItEasy.A.Fake<ITenantClientAccessToken>();
        FakeItEasy.A.CallTo(() => token.AccessToken).Returns(null);
        return new TenantClient(new TenantClientOptions { EndpointUri = _service.BaseUrl, TenantId = "acme" }, token);
    }

    /// <summary>An <see cref="IProgress{T}" /> that reports synchronously (no captured context).</summary>
    private sealed class SyncProgress(Action<CkOperation> report) : IProgress<CkOperation>
    {
        public void Report(CkOperation value) => report(value);
    }

    /// <summary>Records every delay of the wait and lets it elapse at once.</summary>
    private sealed class RecordingTimeProvider : TimeProvider
    {
        private readonly List<TimeSpan> _delays = [];

        public IReadOnlyList<TimeSpan> Delays
        {
            get
            {
                lock (_delays)
                {
                    return _delays.ToArray();
                }
            }
        }

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            lock (_delays)
            {
                _delays.Add(dueTime);
            }

            return new ImmediateTimer(callback, state);
        }

        private sealed class ImmediateTimer : ITimer
        {
            public ImmediateTimer(TimerCallback callback, object? state) =>
                ThreadPool.QueueUserWorkItem(_ => callback(state));

            public bool Change(TimeSpan dueTime, TimeSpan period) => true;

            public void Dispose()
            {
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
