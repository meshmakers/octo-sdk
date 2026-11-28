using System.Reflection;
using System.Text.RegularExpressions;
using GraphQL.Types;
using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants.Methods;

namespace Sdk.SourceGeneration.Tests;

/// <summary>
///     Stream-data archives (Feature F5.4): the method clients a consumer generates from <c>System.StreamData</c> 1.30.0 for
///     raw, time-range and rollup archives - the replacements of the archive methods of <c>IStreamDataServicesClient</c>
///     (lifecycle, rollup control, recompute and backfill, columns and computed columns) plus the methods without an SDK REST
///     wrapper (<c>CreateArchive</c>, <c>RepairComputedColumn</c>, <c>RedactHiddenColumns</c>). The SDK ships no
///     System.StreamData surface; these tests prove what a consumer gets: one client method per CK method (the methods of
///     the abstract <c>Archive</c> on every concrete archive type, no client for the abstract type), the signatures of all
///     methods, the asynchronous <c>-2</c> versions with an operation handle, declared error constants equal to the model's
///     errors, every document valid against the recorded schema of an asset repository tenant with System.StreamData 1.30.0,
///     and answers recorded from that asset repository's method gateway as typed results.
/// </summary>
public class StreamDataMethodClientTests : IClassFixture<MethodClientRuntimeTests.LoopbackGraphQlService>
{
    private const string Ns = "Consumer.DataTransferObjects.System.StreamData.v1";
    private const string Result = "global::Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants.Methods.CkMethodResult";
    private const string Handle = "global::Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants.Methods.CkOperationHandle";
    private const string Transition = $"{Result}<global::{Ns}.RtArchiveTransitionResultRecordDto?>";
    private const string Job = $"{Result}<global::{Ns}.RtRecomputeJobInfoRecordDto?>";

    private const string ArchiveMethods =
        "Activate,AddColumn,AddComputedColumn,DeleteArchive,Disable,Enable,RedactHiddenColumns,RedactHiddenColumns2," +
        "RemoveComputedColumn,RepairComputedColumn,RetryActivation,RetryComputedColumnBackfill,UpdateComputedColumnFormula";

    private static readonly Lazy<SortedDictionary<string, string>> Sources =
        new(() => GeneratorFixtures.Emit(GeneratorFixtures.SystemStreamData, "Consumer"));

    // The archive types derive from System/Entity; a consumer imports System too.
    private static readonly Lazy<Assembly> Generated = new(() => GeneratorFixtures.CompileAndLoad(
        GeneratorFixtures.EmitWithDependencies(GeneratorFixtures.SystemStreamData, "Consumer", GeneratorFixtures.SystemDependency).Values));

    private static readonly Lazy<ISchema> Schema =
        new(() => MethodClientSchemaValidationTests.LoadSchema(GeneratorFixtures.StreamDataSchemaFile));

    private readonly MethodClientRuntimeTests.LoopbackGraphQlService _service;

    public StreamDataMethodClientTests(MethodClientRuntimeTests.LoopbackGraphQlService service)
    {
        _service = service;
        _service.Reset();
    }

    public static TheoryData<string, string> ClientMethods() => new()
    {
        { "RawArchive", ArchiveMethods },
        { "TimeRangeArchive", Sorted(ArchiveMethods + ",CreateArchive") },
        {
            "RollupArchive",
            Sorted(ArchiveMethods + ",BackfillFromSource,BackfillFromSource2,CreateArchive,Freeze,Recompute,Recompute2,RewindWatermark,Unfreeze")
        }
    };

    [Theory]
    [MemberData(nameof(ClientMethods))]
    public void Client_HasOneMethodPerCkMethod(string type, string methods)
    {
        var code = Sources.Value[$"{Ns}.Type.{type}MethodClient.g.cs"];

        Assert.Contains($"public partial class Rt{type}MethodClient : global::Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants.Methods.CkMethodClientBase", code);
        Assert.Equal(methods, Sorted(ClientMethodNames(code)));
        Assert.DoesNotContain("Unsupported by Generator", code);
    }

    [Fact]
    public void OnlyTheConcreteArchiveTypesHaveAClient()
    {
        // Archive is abstract: its methods are called on RawArchive, TimeRangeArchive or RollupArchive. RecomputeJob has none.
        var clients = Sources.Value.Keys.Where(k => k.EndsWith("MethodClient.g.cs", StringComparison.Ordinal))
            .Select(k => k[(Ns.Length + ".Type.".Length)..^"MethodClient.g.cs".Length])
            .ToList();

        Assert.Equal(["RawArchive", "RollupArchive", "TimeRangeArchive"], clients);
    }

    [Theory]
    // Replacements of the REST archive methods of IStreamDataServicesClient, by rtId.
    [InlineData("RawArchive", $"Task<{Transition}> ActivateAsync(string rtId, string? idempotencyKey = null)")]
    [InlineData("RawArchive", $"Task<{Transition}> DisableAsync(string rtId, string? idempotencyKey = null)")]
    [InlineData("RawArchive", $"Task<{Transition}> EnableAsync(string rtId, string? idempotencyKey = null)")]
    [InlineData("RawArchive", $"Task<{Transition}> RetryActivationAsync(string rtId, string? idempotencyKey = null)")]
    [InlineData("RawArchive", $"Task<{Result}<bool?>> DeleteArchiveAsync(string rtId, string? idempotencyKey = null)")]
    [InlineData("RawArchive", $"Task<{Transition}> AddColumnAsync(string rtId, string path, bool? indexed = null, string? idempotencyKey = null)")]
    [InlineData("RawArchive", $"Task<{Transition}> AddComputedColumnAsync(string rtId, string name, string formula, global::{Ns}.RtCkComputedColumnResultTypeEnum resultType, bool? indexed = null, string? idempotencyKey = null)")]
    [InlineData("RawArchive", $"Task<{Transition}> RemoveComputedColumnAsync(string rtId, string name, string? idempotencyKey = null)")]
    [InlineData("RawArchive", $"Task<{Transition}> UpdateComputedColumnFormulaAsync(string rtId, string name, string formula, string? idempotencyKey = null)")]
    [InlineData("RawArchive", $"Task<{Job}> RetryComputedColumnBackfillAsync(string rtId, string name, string? idempotencyKey = null)")]
    [InlineData("RollupArchive", $"Task<{Transition}> FreezeAsync(string rtId, global::System.DateTime until, string? idempotencyKey = null)")]
    [InlineData("RollupArchive", $"Task<{Transition}> UnfreezeAsync(string rtId, bool? acceptGaps = null, string? idempotencyKey = null)")]
    [InlineData("RollupArchive", $"Task<{Transition}> RewindWatermarkAsync(string rtId, global::System.DateTime toBucketEnd, string? idempotencyKey = null)")]
    // Recompute and backfill: -1 answers the job (deprecated in the model), -2 an operation handle whose result is the job.
    [InlineData("RollupArchive", $"Task<{Job}> RecomputeAsync(string rtId, global::System.DateTime @from, global::System.DateTime to, string? rtIdScope = null, string? idempotencyKey = null)")]
    [InlineData("RollupArchive", $"Task<{Job}> BackfillFromSourceAsync(string rtId, string? idempotencyKey = null)")]
    [InlineData("RollupArchive", $"Task<{Result}<{Handle}<global::{Ns}.RtRecomputeJobInfoRecordDto?>>> Recompute2Async(string rtId, global::System.DateTime @from, global::System.DateTime to, string? rtIdScope = null, string? idempotencyKey = null)")]
    [InlineData("RollupArchive", $"Task<{Result}<{Handle}<global::{Ns}.RtRecomputeJobInfoRecordDto?>>> BackfillFromSource2Async(string rtId, string? idempotencyKey = null)")]
    // Without an SDK REST method: the static creates (no rtId, the new archive's rtId as result), repair and redaction.
    [InlineData("RollupArchive", $"Task<{Result}<string?>> CreateArchiveAsync(global::{Ns}.RtRollupArchiveCreateInputRecordMutationDto archive, string? idempotencyKey = null)")]
    [InlineData("TimeRangeArchive", $"Task<{Result}<string?>> CreateArchiveAsync(global::{Ns}.RtTimeRangeArchiveCreateInputRecordMutationDto archive, string? idempotencyKey = null)")]
    [InlineData("RawArchive", $"Task<{Job}> RepairComputedColumnAsync(string rtId, string name, global::System.DateTime? @from = null, global::System.DateTime? to = null, string? idempotencyKey = null)")]
    [InlineData("RawArchive", $"Task<{Result}<global::{Ns}.RtArchiveRedactionResultRecordDto?>> RedactHiddenColumns2Async(string rtId, bool confirmDataLoss, global::System.Collections.Generic.IReadOnlyList<string> expectedColumns, string? idempotencyKey = null)")]
    public void Client_Signature(string type, string signature)
    {
        Assert.Contains(signature, Sources.Value[$"{Ns}.Type.{type}MethodClient.g.cs"]);
    }

    [Theory]
    [InlineData("RawArchive")]
    [InlineData("TimeRangeArchive")]
    [InlineData("RollupArchive")]
    public void ArchiveClients_ShareTheSignaturesOfTheAbstractBase(string type)
    {
        // The Archive methods (lifecycle, columns) are declared once on the abstract base and appear on every concrete client.
        var code = Sources.Value[$"{Ns}.Type.{type}MethodClient.g.cs"];

        foreach (var method in new[] { "ActivateAsync", "DisableAsync", "EnableAsync", "RetryActivationAsync" })
        {
            Assert.Contains($"Task<{Transition}> {method}(string rtId, string? idempotencyKey = null)", code);
        }

        Assert.Contains($"Task<{Result}<bool?>> DeleteArchiveAsync(string rtId, string? idempotencyKey = null)", code);
    }

    public static TheoryData<string, string, string> DeclaredErrorsOfTheModel()
    {
        var (model, _, _) = GeneratorFixtures.Restore(GeneratorFixtures.SystemStreamData);
        var data = new TheoryData<string, string, string>();
        foreach (var type in new[] { "Archive", "TimeRangeArchive", "RollupArchive" })
        {
            var ckType = model.Types!.Single(t => t.TypeId.Name == type);
            foreach (var method in ckType.Methods!.Where(m => m.Errors is { Count: > 0 }))
            {
                // The Archive methods are declared on the abstract base; check them on one concrete client.
                var client = type == "Archive" ? "RawArchive" : type;
                data.Add(client, MethodClassName(method.MethodId), string.Join(',', method.Errors!.Select(e => e.Code)));
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(DeclaredErrorsOfTheModel))]
    public void DeclaredErrors_AreConstants_InTheOrderOfTheModel(string client, string method, string codes)
    {
        var errors = Generated.Value.GetType($"{Ns}.Rt{client}MethodClient+{method}Errors");

        Assert.NotNull(errors);
        Assert.Equal(codes.Split(','), errors.GetFields().Select(f => (string)f.GetValue(null)!));
    }

    [Fact]
    public void DeclaredErrors_CoverEveryMethod_AndTheRecomputeFailure()
    {
        // Guards the theory above against an empty model read: all 22 methods declare errors, and the asynchronous -2
        // versions end a failed job with STREAMDATA_RECOMPUTE_FAILED.
        var rows = DeclaredErrorsOfTheModel().Select(row => row.Data).ToList();

        Assert.Equal(22, rows.Count);
        Assert.Contains(rows, r => r.Item2 == "Recompute2" && r.Item3.Split(',').Contains("STREAMDATA_RECOMPUTE_FAILED"));
        Assert.Contains(rows, r => r.Item2 == "BackfillFromSource2" && r.Item3.Split(',').Contains("STREAMDATA_RECOMPUTE_FAILED"));
        Assert.All(rows, r => Assert.Contains("STREAMDATA_NOT_ENABLED", r.Item3.Split(',')));
    }

    [Fact]
    public void GeneratedSources_CompileWithoutWarnings()
    {
        Assert.All(ClientMethods(), row => Assert.NotNull(Generated.Value.GetType($"{Ns}.Rt{row.Data.Item1}MethodClient")));
    }

    public static TheoryData<string, string> StreamDataDocuments()
    {
        var data = new TheoryData<string, string>();
        foreach (var (hint, code) in Sources.Value.Where(s => s.Key.EndsWith("MethodClient.g.cs", StringComparison.Ordinal)))
        {
            foreach (Match match in Regex.Matches(code, "public const string (?<name>\\w+)Document = \"(?<doc>[^\"]*)\";"))
            {
                data.Add($"{hint}:{match.Groups["name"].Value}", match.Groups["doc"].Value);
            }
        }

        return data;
    }

    [Fact]
    public void StreamDataDocuments_CoverEveryClientMethod()
    {
        // 13 + 14 + 21 client methods, one document each.
        Assert.Equal(48, StreamDataDocuments().Count());
    }

    [Theory]
    [MemberData(nameof(StreamDataDocuments))]
    public async Task StreamDataDocument_ValidatesAgainstTheRecordedSchemaOfATenantWithSystemStreamData(string method,
        string document)
    {
        var errors = await MethodClientSchemaValidationTests.DocumentErrorsAsync(Schema.Value, document);

        Assert.True(errors.Count == 0, method + ": " + string.Join("; ", errors));
    }

    [Theory]
    // The abstract Archive has no mutation field; its methods live on the concrete archive types.
    [InlineData("mutation M($rtId: OctoObjectId!, $idempotencyKey: String) { runtime { systemStreamDataArchives { activate(rtId: $rtId, idempotencyKey: $idempotencyKey) { invocationId } } } }")]
    // Recompute-2 is asynchronous: it answers an operation handle, no result.
    [InlineData("mutation M($rtId: OctoObjectId!, $input: SystemStreamDataRollupArchiveRecompute2MethodInput!, $idempotencyKey: String) { runtime { systemStreamDataRollupArchives { recompute2(rtId: $rtId, input: $input, idempotencyKey: $idempotencyKey) { invocationId result { jobRtId } } } } }")]
    // CreateArchive is static: no rtId argument.
    [InlineData("mutation M($rtId: OctoObjectId!, $input: SystemStreamDataTimeRangeArchiveCreateArchiveMethodInput!) { runtime { systemStreamDataTimeRangeArchives { createArchive(rtId: $rtId, input: $input) { invocationId result } } } }")]
    // Recompute is declared on RollupArchive only.
    [InlineData("mutation M($rtId: OctoObjectId!, $input: SystemStreamDataRollupArchiveRecompute2MethodInput!) { runtime { systemStreamDataTimeRangeArchives { recompute2(rtId: $rtId, input: $input) { invocationId } } } }")]
    public async Task BrokenStreamDataDocument_IsRejected_SoTheGateIsReal(string document)
    {
        Assert.NotEmpty(await MethodClientSchemaValidationTests.DocumentErrorsAsync(Schema.Value, document));
    }

    // Answers recorded from the asset repository: the generated documents executed with the client's variables on the real
    // schema and method gateway of a stream-data tenant with System.StreamData 1.30.0 (local stack, see docs/method-clients.md).

    [Fact]
    public async Task RecordedAnswer_CreateTimeRangeArchive_ReturnsTheNewRtId()
    {
        _service.Respond(200, Recorded("time-range-create-archive-success"));
        var input = CreateInput("RtTimeRangeArchiveCreateInputRecordMutationDto",
            ("RtWellKnownName", "job-rows"), ("TargetCkTypeId", "System.StreamData/RecomputeJob"), ("PeriodMs", (int?)900000));

        var result = await CallAsync("TimeRangeArchive", "CreateArchiveAsync", input, "key-create");

        Assert.Equal("6b0a88eafa75fad21127ffa9", ResultValue(result));
        var body = Assert.Single(_service.Bodies);
        Assert.Contains("systemStreamDataTimeRangeArchives { createArchive(input: $input, idempotencyKey: $idempotencyKey)", body, StringComparison.Ordinal);
        Assert.Contains("\"targetCkTypeId\":\"System.StreamData/RecomputeJob\"", body, StringComparison.Ordinal);
        Assert.Contains("\"idempotencyKey\":\"key-create\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("\"rtId\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RecordedAnswer_CreateRollupArchive_ReturnsTheNewRtId()
    {
        _service.Respond(200, Recorded("rollup-create-archive-success"));
        // The record members are Int64 in the model; the DTO generator maps a nullable Int64 to int? today (known
        // limitation, see the CHANGELOG), so bucket widths above Int32.MaxValue (calendar months) cannot be sent yet.
        var input = CreateInput("RtRollupArchiveCreateInputRecordMutationDto", ("BucketSizeMs", (int?)3600000), ("WatermarkLagMs", (int?)0));

        var result = await CallAsync("RollupArchive", "CreateArchiveAsync", input);

        Assert.Equal("6b0a88fefa75fad21127ffb3", ResultValue(result));
        Assert.Contains("\"bucketSizeMs\":3600000", Assert.Single(_service.Bodies), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RecordedAnswer_Activate_ReturnsTheTransition()
    {
        _service.Respond(200, Recorded("time-range-activate-success"));

        var result = await CallAsync("TimeRangeArchive", "ActivateAsync", "6b0a88eafa75fad21127ffa9");

        var value = ResultValue(result)!;
        Assert.Equal("6b0a88eafa75fad21127ffa9", Property(value, "ArchiveRtId"));
        Assert.Equal("Activate", Property(value, "Transition"));
        Assert.Equal("Activated", Property(value, "Status")!.ToString());
        Assert.Contains("systemStreamDataTimeRangeArchives { activate(rtId: $rtId, idempotencyKey: $idempotencyKey)",
            Assert.Single(_service.Bodies), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RecordedAnswer_AddColumn_SendsThePathAndIndex()
    {
        _service.Respond(200, Recorded("time-range-add-column-success"));

        var result = await CallAsync("TimeRangeArchive", "AddColumnAsync", "6b0a88eafa75fad21127ffa9", "WindowsProcessed", true);

        Assert.Equal("AddArchiveColumn", Property(ResultValue(result)!, "Transition"));
        Assert.Contains("\"input\":{\"path\":\"WindowsProcessed\",\"indexed\":true}", Assert.Single(_service.Bodies), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RecordedAnswer_DisableFromCreated_IsATypedResultWithTheTransitionParameters()
    {
        _service.Respond(200, Recorded("time-range-disable-invalid-transition"));

        var result = (CkMethodResult)await CallAsync("TimeRangeArchive", "DisableAsync", "6b0a88f4fa75fad21127ffaf");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsError(ErrorConstant("TimeRangeArchive", "Disable", "StreamdataInvalidTransition")));
        Assert.Equal("Created", result.Error!.Parameters["fromStatus"]);
        Assert.Equal("disable", result.Error.Parameters["attemptedTransition"]);
        Assert.Equal("Cannot disable archive '6b0a88f4fa75fad21127ffaf' from status Created.", result.Error.Parameters["message"]);
    }

    [Fact]
    public async Task RecordedAnswer_RetryActivation_ActivationFailedIsATypedResult()
    {
        _service.Respond(200, Recorded("time-range-retry-activation-failed"));

        var result = (CkMethodResult)await CallAsync("TimeRangeArchive", "RetryActivationAsync", "6b0a88dafa75fad21127ffa3");

        Assert.True(result.IsError(ErrorConstant("TimeRangeArchive", "RetryActivation", "StreamdataActivationFailed")));
        Assert.StartsWith("Activation of archive '6b0a88dafa75fad21127ffa3' failed", result.Error!.Parameters["message"], StringComparison.Ordinal);
    }

    [Fact]
    public async Task RecordedAnswer_ActivateUnknownArchive_ThrowsTargetNotFound()
    {
        _service.Respond(200, Recorded("time-range-activate-target-not-found"));

        var e = await Assert.ThrowsAsync<CkMethodCallException>(() => CallAsync("TimeRangeArchive", "ActivateAsync", "6b0a88f4fa75fad21127ff00"));

        Assert.Equal("METHOD_TARGET_NOT_FOUND", e.Code);
        Assert.Equal("System.StreamData/Archive.Activate-1", e.MethodId);
        Assert.IsAssignableFrom<QlQueryErrorException>(e);
    }

    [Fact]
    public async Task RecordedAnswer_DeleteArchive_ReturnsTrue()
    {
        _service.Respond(200, Recorded("time-range-delete-archive-success"));

        var result = await CallAsync("TimeRangeArchive", "DeleteArchiveAsync", "6b0a88f4fa75fad21127ffaf");

        Assert.Equal(true, ResultValue(result));
    }

    [Fact]
    public async Task RecordedAnswer_Freeze_SendsTheInstant()
    {
        _service.Respond(200, Recorded("rollup-freeze-success"));

        var result = await CallAsync("RollupArchive", "FreezeAsync", "6b0a88fefa75fad21127ffb3",
            new DateTime(2026, 11, 27, 12, 0, 0, DateTimeKind.Utc));

        Assert.Equal("Freeze", Property(ResultValue(result)!, "Transition"));
        Assert.Contains("\"input\":{\"until\":\"2026-11-27T12:00:00", Assert.Single(_service.Bodies), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RecordedAnswer_Recompute2_IsAHandle_AndTheWaitReturnsTheCompletedJob()
    {
        // Accepted answer and final operation of the same run (a recompute of the activated rollup).
        _service.Respond(200, Recorded("rollup-recompute2-accepted-activated"));
        var handle = await StartAsync("Recompute2Async", "6b0a88fefa75fad21127ffb3",
            new DateTime(2026, 11, 27, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 11, 28, 0, 0, 0, DateTimeKind.Utc), null, "key-rc");

        Assert.Equal("55fd5a7137614c289ba808bc7b36b5e2", handle.OperationId);
        Assert.Equal(5, handle.RetryAfterSeconds);
        Assert.Equal("System.StreamData/RollupArchive.Recompute-2", handle.MethodId);
        var body = Assert.Single(_service.Bodies);
        Assert.Contains("\"input\":{\"from\":\"2026-11-27T00:00:00", body, StringComparison.Ordinal);
        Assert.Contains("operation { operationId acceptedAtUtc retryAfterSeconds expiresAtUtc }", body, StringComparison.Ordinal);

        _service.Reset();
        _service.RespondInOrder(Recorded("operation-recompute-succeeded"));
        var result = await WaitAsync(handle);

        Assert.True(result.IsSuccess);
        Assert.Contains("\"operationId\":\"55fd5a7137614c289ba808bc7b36b5e2\"", Assert.Single(_service.Bodies), StringComparison.Ordinal);
        var job = result.GetType().GetProperty("Value")!.GetValue(result)!;
        Assert.Equal("6b0a8911fa75fad21127ffbb", Property(job, "JobRtId"));
        Assert.Equal("Completed", Property(job, "State")!.ToString());
        Assert.Equal(24L, Convert.ToInt64(Property(job, "WindowsProcessed")));
        Assert.Equal(209L, Convert.ToInt64(Property(job, "DurationMs")));
    }

    [Fact]
    public async Task RecordedAnswer_Recompute2_FailedJobIsTheDeclaredRecomputeFailure()
    {
        // A recompute of a rollup that is not activated: the call is accepted, the job ends Failed and the operation with
        // STREAMDATA_RECOMPUTE_FAILED (job id and reason as parameters) - a typed result, not an exception.
        _service.Respond(200, Recorded("rollup-recompute2-accepted"));
        var handle = await StartAsync("Recompute2Async", "6b0a88fefa75fad21127ffb3",
            new DateTime(2026, 11, 27, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 11, 28, 0, 0, 0, DateTimeKind.Utc), null, null);
        _service.Reset();
        _service.RespondInOrder(Recorded("operation-recompute-failed"));

        var result = await WaitAsync(handle);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsError(ErrorConstant("RollupArchive", "Recompute2", "StreamdataRecomputeFailed")));
        Assert.Equal("6b0a8908fa75fad21127ffb6", result.Error!.Parameters["jobRtId"]);
        Assert.StartsWith("Archive is not activated", result.Error.Parameters["reason"], StringComparison.Ordinal);
    }

    [Fact]
    public async Task RecordedAnswer_BackfillFromSource2_WithoutSourceDataSucceedsWithoutAJob()
    {
        _service.Respond(200, Recorded("rollup-backfill-from-source2-accepted"));
        var handle = await StartAsync("BackfillFromSource2Async", "6b0a88fefa75fad21127ffb3", "key-bf");
        _service.Reset();
        _service.RespondInOrder(Recorded("operation-backfill-succeeded"));

        var result = await WaitAsync(handle);

        // The source archive holds no data: the operation succeeds with no result, as BackfillFromSource-1 answers null.
        Assert.True(result.IsSuccess);
        Assert.Null(result.GetType().GetProperty("Value")!.GetValue(result));
        Assert.Equal("ae9db76d1e4e47ea8beb3b81e7811ccb", handle.OperationId);
    }

    private static string Sorted(string commaSeparated) => Sorted(commaSeparated.Split(','));

    private static string Sorted(IEnumerable<string> names) => string.Join(',', names.OrderBy(s => s, StringComparer.Ordinal));

    /// <summary>The method class name of a method id: <c>Recompute-2</c> becomes <c>Recompute2</c>, <c>Activate-1</c> <c>Activate</c>.</summary>
    private static string MethodClassName(string methodId)
    {
        var dash = methodId.LastIndexOf('-');
        var version = methodId[(dash + 1)..];
        return methodId[..dash] + (version == "1" ? "" : version);
    }

    private static List<string> ClientMethodNames(string code) =>
        Regex.Matches(code, @"public global::System\.Threading\.Tasks\.Task<.+?> (\w+)Async\(")
            .Select(m => m.Groups[1].Value).ToList();

    private async Task<object> CallAsync(string type, string method, params object?[] arguments)
    {
        var clientType = Generated.Value.GetType($"{Ns}.Rt{type}MethodClient")!;
        var client = Activator.CreateInstance(clientType, RealTenantClient())!;
        var info = clientType.GetMethods().Single(m => m.Name == method);
        // Trailing optional parameters (and the idempotency key) not given by the test are passed as null.
        var padded = arguments.Concat(Enumerable.Repeat<object?>(null, info.GetParameters().Length - arguments.Length)).ToArray();
        var task = (Task)info.Invoke(client, padded)!;
        await task;
        return task.GetType().GetProperty("Result")!.GetValue(task)!;
    }

    /// <summary>Calls an asynchronous method of the rollup client and returns the operation handle of its accepted answer.</summary>
    private async Task<CkOperationHandle> StartAsync(string method, params object?[] arguments)
    {
        var result = await CallAsync("RollupArchive", method, arguments);
        Assert.True(((CkMethodResult)result).IsSuccess);
        return (CkOperationHandle)result.GetType().GetProperty("Value")!.GetValue(result)!;
    }

    /// <summary>Calls the typed <see cref="CkOperationsClient.WaitForOperationAsync{TResult}" /> for the handle's result type.</summary>
    private async Task<CkMethodResult> WaitAsync(CkOperationHandle handle)
    {
        var resultType = handle.GetType().GetGenericArguments()[0];
        var method = typeof(CkOperationsClient).GetMethods()
            .Single(m => m.Name == nameof(CkOperationsClient.WaitForOperationAsync) && m.IsGenericMethodDefinition)
            .MakeGenericMethod(resultType);
        // Read at once: the recorded operation is already final.
        var options = new CkOperationWaitOptions { WaitBeforeFirstRead = false, Timeout = TimeSpan.FromSeconds(30) };
        var task = (Task)method.Invoke(new CkOperationsClient(RealTenantClient()),
            [handle, null, options, TestContext.Current.CancellationToken])!;
        await task;
        return (CkMethodResult)task.GetType().GetProperty("Result")!.GetValue(task)!;
    }

    /// <summary>A create input record of the generated model with the given properties (the rest stays unset).</summary>
    private static object CreateInput(string recordType, params (string Name, object? Value)[] values)
    {
        var type = Generated.Value.GetType($"{Ns}.{recordType}")!;
        var input = Activator.CreateInstance(type)!;
        foreach (var (name, value) in values)
        {
            type.GetProperty(name)!.SetValue(input, value);
        }

        return input;
    }

    private static object? ResultValue(object result)
    {
        Assert.True(((CkMethodResult)result).IsSuccess);
        return result.GetType().GetProperty("Value")!.GetValue(result);
    }

    private static object? Property(object value, string name) => value.GetType().GetProperty(name)!.GetValue(value);

    private static string ErrorConstant(string type, string method, string name) =>
        (string)Generated.Value.GetType($"{Ns}.Rt{type}MethodClient+{method}Errors")!.GetField(name)!.GetValue(null)!;

    private static string Recorded(string name) =>
        File.ReadAllText(Path.Combine(GeneratorFixtures.FixtureDirectory, $"assetrepo-streamdata-answer-{name}.json"));

    private ITenantClient RealTenantClient()
    {
        var token = A.Fake<ITenantClientAccessToken>();
        A.CallTo(() => token.AccessToken).Returns(null);
        return new TenantClient(new TenantClientOptions { EndpointUri = _service.BaseUrl, TenantId = "acme" }, token);
    }
}
