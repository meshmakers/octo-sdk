using System.Reflection;
using System.Text.RegularExpressions;
using GraphQL.Types;
using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants.Methods;

namespace Sdk.SourceGeneration.Tests;

/// <summary>
///     AB#9052 (communication wave 3, Feature F5.3): the method clients a consumer generates from
///     <c>System.Communication</c> 4.19.0 for adapters, adapter pools, applications, pipelines, data flows, deployment sites
///     and pipeline triggers - the replacements of the operation methods of <c>ICommunicationServicesClient</c> plus the
///     methods without an SDK REST wrapper (<c>DeployUpdate</c>, <c>Wake</c>, the pool mirrors, the lease, the service
///     account reconcile). The SDK ships no System.Communication surface (as for System.Identity, AB#8416); these tests prove
///     what a consumer gets: one client method per CK method (the four <c>DeployableWorkload</c> methods on every concrete
///     workload type, no client for the abstract types), the signatures of the replacements, declared error constants equal
///     to the model's errors, every document valid against the recorded schema of an asset repository tenant with
///     System.Communication 4.19.0, and answers recorded from that asset repository's method gateway (scripted handler;
///     steps in docs/method-clients.md) as typed results.
/// </summary>
public class CommunicationMethodClientTests : IClassFixture<MethodClientRuntimeTests.LoopbackGraphQlService>
{
    private const string Ns = "Consumer.DataTransferObjects.System.Communication.v4";
    private const string TargetRtId = "6aff6ef325266ca7cb262c18";
    private const string Result = "global::Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants.Methods.CkMethodResult";
    private const string Workload = "Deploy,SetChartVersion,Undeploy,Wake";

    private static readonly Lazy<SortedDictionary<string, string>> Sources =
        new(() => GeneratorFixtures.Emit(GeneratorFixtures.SystemCommunication, "Consumer"));

    // The System.Communication types reference System types (Configuration); a consumer gets those from the System model file.
    private static readonly Lazy<Assembly> Generated = new(() => GeneratorFixtures.CompileAndLoad(
        GeneratorFixtures.EmitWithDependencies(GeneratorFixtures.SystemCommunication, "Consumer", GeneratorFixtures.SystemDependency).Values));

    private static readonly Lazy<ISchema> Schema =
        new(() => MethodClientSchemaValidationTests.LoadSchema(GeneratorFixtures.CommunicationSchemaFile));

    private readonly MethodClientRuntimeTests.LoopbackGraphQlService _service;

    public CommunicationMethodClientTests(MethodClientRuntimeTests.LoopbackGraphQlService service)
    {
        _service = service;
        _service.Reset();
    }

    public static TheoryData<string, string> ClientMethods() => new()
    {
        { "Adapter", "Deploy,DeployUpdate,ReconcileServiceAccount,RotateServiceAccountSecret,SetChartVersion,Undeploy,Wake" },
        { "AdapterPool", "CancelQueuedExecution,Deploy,GrantLease,PublishMirrors,RefreshMirrors,Scale,SetChartVersion,Undeploy,Wake" },
        { "Application", Workload },
        { "Pipeline", "Deploy,Execute,MoveToAdapter,SetDebugging" },
        { "DataFlow", "Deploy,Undeploy" },
        { "DeploymentSite", "Deploy,Undeploy" },
        { "PipelineTrigger", "DeployAll,UndeployAll" }
    };

    [Theory]
    [MemberData(nameof(ClientMethods))]
    public void Client_HasOneMethodPerCkMethod(string type, string methods)
    {
        var code = Sources.Value[$"{Ns}.Type.{type}MethodClient.g.cs"];

        Assert.Contains($"public partial class Rt{type}MethodClient : global::Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants.Methods.CkMethodClientBase", code);
        Assert.Equal(methods.Split(','), ClientMethodNames(code));
        Assert.DoesNotContain("Unsupported by Generator", code);
    }

    [Fact]
    public void OnlyTheConcreteTypesWithMethodsHaveAClient()
    {
        // DeployableWorkload and DeployableEntity are abstract: their methods are called on Adapter, AdapterPool or Application.
        var clients = Sources.Value.Keys.Where(k => k.EndsWith("MethodClient.g.cs", StringComparison.Ordinal))
            .Select(k => k[(Ns.Length + ".Type.".Length)..^"MethodClient.g.cs".Length])
            .ToList();

        Assert.Equal(["Adapter", "AdapterPool", "Application", "DataFlow", "DeploymentSite", "Pipeline", "PipelineTrigger"], clients);
    }

    [Theory]
    // Replacements of the REST operation methods of ICommunicationServicesClient (by rtId; static methods without one).
    [InlineData("Pipeline", $"Task<{Result}> DeployAsync(string rtId, string? adapterRtId = null, string? pipelineDefinition = null, string? idempotencyKey = null)")]
    [InlineData("Pipeline", $"Task<{Result}<global::{Ns}.RtPipelineExecutionStartResultRecordDto?>> ExecuteAsync(string rtId, string? input = null, bool? isDryRun = null, string? idempotencyKey = null)")]
    [InlineData("Pipeline", $"Task<{Result}<global::{Ns}.RtPipelineDebugResultRecordDto?>> SetDebuggingAsync(string rtId, bool enabled, string? idempotencyKey = null)")]
    [InlineData("Pipeline", $"Task<{Result}<global::{Ns}.RtPipelineMoveResultRecordDto?>> MoveToAdapterAsync(string rtId, string targetAdapterRtId, bool? redeploy = null, string? idempotencyKey = null)")]
    [InlineData("Adapter", $"Task<{Result}<global::{Ns}.RtServiceAccountSecretRotationResultRecordDto?>> RotateServiceAccountSecretAsync(string rtId, string? idempotencyKey = null)")]
    [InlineData("Adapter", $"Task<{Result}<global::{Ns}.RtServiceAccountReconcileResultRecordDto?>> ReconcileServiceAccountAsync(string rtId, string? idempotencyKey = null)")]
    [InlineData("Adapter", $"Task<{Result}> DeployUpdateAsync(string rtId, string? idempotencyKey = null)")]
    [InlineData("AdapterPool", $"Task<{Result}> CancelQueuedExecutionAsync(string rtId, string executionId, string? idempotencyKey = null)")]
    [InlineData("AdapterPool", $"Task<{Result}<global::{Ns}.RtAdapterPoolLeaseResultRecordDto?>> GrantLeaseAsync(string rtId, string borrowerTenantId, string adapterRtId, string? executionId = null, int? ttlSeconds = null, string? idempotencyKey = null)")]
    [InlineData("AdapterPool", $"Task<{Result}<global::{Ns}.RtAdapterPoolMirrorSyncResultRecordDto?>> RefreshMirrorsAsync(string? idempotencyKey = null)")]
    [InlineData("AdapterPool", $"Task<{Result}<global::{Ns}.RtAdapterPoolMirrorSyncResultRecordDto?>> PublishMirrorsAsync(string? idempotencyKey = null)")]
    [InlineData("AdapterPool", $"Task<{Result}<global::{Ns}.RtAdapterPoolScaleResultRecordDto?>> ScaleAsync(string rtId, int replicas, string? idempotencyKey = null)")]
    [InlineData("DataFlow", $"Task<{Result}> DeployAsync(string rtId, string? idempotencyKey = null)")]
    [InlineData("DataFlow", $"Task<{Result}> UndeployAsync(string rtId, string? idempotencyKey = null)")]
    [InlineData("DeploymentSite", $"Task<{Result}> DeployAsync(string rtId, string? idempotencyKey = null)")]
    [InlineData("DeploymentSite", $"Task<{Result}> UndeployAsync(string rtId, string? idempotencyKey = null)")]
    [InlineData("PipelineTrigger", $"Task<{Result}> DeployAllAsync(string? idempotencyKey = null)")]
    [InlineData("PipelineTrigger", $"Task<{Result}> UndeployAllAsync(string? idempotencyKey = null)")]
    public void Client_Signature_OfAReplacement(string type, string signature)
    {
        Assert.Contains(signature, Sources.Value[$"{Ns}.Type.{type}MethodClient.g.cs"]);
    }

    [Theory]
    [InlineData("Adapter")]
    [InlineData("AdapterPool")]
    [InlineData("Application")]
    public void WorkloadClients_ShareTheSignaturesOfTheAbstractBase(string type)
    {
        // DeployableWorkload.Deploy/Undeploy/SetChartVersion/Wake replace DeployWorkload, UndeployWorkload and
        // UpdateWorkloadChartVersion; the client is the one of the workload's concrete type.
        var code = Sources.Value[$"{Ns}.Type.{type}MethodClient.g.cs"];

        Assert.Contains($"Task<{Result}<global::{Ns}.RtWorkloadDeployResultRecordDto?>> DeployAsync(string rtId, string? idempotencyKey = null)", code);
        Assert.Contains($"Task<{Result}> UndeployAsync(string rtId, string? idempotencyKey = null)", code);
        Assert.Contains($"Task<{Result}> SetChartVersionAsync(string rtId, string? chartVersion = null, string? idempotencyKey = null)", code);
        Assert.Contains($"Task<{Result}> WakeAsync(string rtId, string? idempotencyKey = null)", code);
    }

    public static TheoryData<string, string, string> DeclaredErrorsOfTheModel()
    {
        var (model, _, _) = GeneratorFixtures.Restore(GeneratorFixtures.SystemCommunication);
        var data = new TheoryData<string, string, string>();
        foreach (var type in new[] { "Adapter", "AdapterPool", "DeployableWorkload", "Pipeline", "DataFlow", "DeploymentSite", "PipelineTrigger" })
        {
            var ckType = model.Types!.Single(t => t.TypeId.Name == type);
            foreach (var method in ckType.Methods!.Where(m => m.Errors is { Count: > 0 }))
            {
                // The workload methods are declared on the abstract base; check them on one concrete client.
                var client = type == "DeployableWorkload" ? "Application" : type;
                data.Add(client, method.MethodId[..method.MethodId.LastIndexOf('-')],
                    string.Join(',', method.Errors!.Select(e => e.Code)));
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
    public void DeclaredErrors_CoverTheWave3Codes()
    {
        // Guards the theory above against an empty model read: the known codes of the wave are there.
        var codes = DeclaredErrorsOfTheModel().SelectMany(row => row.Data.Item3.Split(',')).ToHashSet();

        Assert.Superset(new HashSet<string>
        {
            "ADAPTER_CONFIGURATION_DEPLOY_FAILED", "SERVICE_ACCOUNT_RECONCILE_FAILED", "SERVICE_ACCOUNT_ROTATION_FAILED",
            "EXECUTION_ALREADY_LEASED", "QUEUED_EXECUTION_NOT_FOUND", "COMMUNICATION_DISABLED", "ADAPTER_POOL_SCALE_FAILED",
            "WORKLOAD_WAKE_FAILED", "WORKLOAD_CHART_VERSION_UPDATE_FAILED", "WORKLOAD_DEPLOY_FAILED", "WORKLOAD_UNDEPLOY_FAILED",
            "DEPLOYMENT_SITE_DEPLOY_FAILED", "DEPLOYMENT_SITE_UNDEPLOY_FAILED", "PIPELINE_DEPLOY_REJECTED", "PIPELINE_DEPLOY_FAILED",
            "PIPELINE_DEBUG_CHANGE_FAILED", "DATA_FLOW_DEPLOY_FAILED", "DATA_FLOW_UNDEPLOY_FAILED", "PIPELINE_ADAPTER_NOT_ASSIGNED",
            "PIPELINE_EXECUTION_START_FAILED", "PIPELINE_MOVE_FAILED", "PIPELINE_TRIGGERS_DEPLOY_FAILED", "PIPELINE_TRIGGERS_UNDEPLOY_FAILED"
        }, codes);
    }

    [Fact]
    public void GeneratedSources_CompileWithoutWarnings()
    {
        Assert.All(ClientMethods(), row => Assert.NotNull(Generated.Value.GetType($"{Ns}.Rt{row.Data.Item1}MethodClient")));
    }

    public static TheoryData<string, string> CommunicationDocuments()
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
    public void CommunicationDocuments_CoverEveryClientMethod()
    {
        // 7 + 9 + 4 + 4 + 2 + 2 + 2 client methods, one document each.
        Assert.Equal(30, CommunicationDocuments().Count());
    }

    [Theory]
    [MemberData(nameof(CommunicationDocuments))]
    public async Task CommunicationDocument_ValidatesAgainstTheRecordedSchemaOfATenantWithSystemCommunication(string method,
        string document)
    {
        var errors = await MethodClientSchemaValidationTests.DocumentErrorsAsync(Schema.Value, document);

        Assert.True(errors.Count == 0, method + ": " + string.Join("; ", errors));
    }

    [Theory]
    // Scale has a required parameter: its input is non-null in the schema.
    [InlineData("mutation M($rtId: OctoObjectId!, $input: SystemCommunicationAdapterPoolScaleMethodInput, $idempotencyKey: String) { runtime { systemCommunicationAdapterPools { scale(rtId: $rtId, input: $input, idempotencyKey: $idempotencyKey) { invocationId } } } }")]
    // DeployAll is static: no rtId argument.
    [InlineData("mutation M($rtId: OctoObjectId!, $idempotencyKey: String) { runtime { systemCommunicationPipelineTriggers { deployAll(rtId: $rtId, idempotencyKey: $idempotencyKey) { invocationId } } } }")]
    // The abstract DeployableWorkload has no mutation field; the workload methods live on the concrete types.
    [InlineData("mutation M($rtId: OctoObjectId!, $idempotencyKey: String) { runtime { systemCommunicationDeployableWorkloads { wake(rtId: $rtId, idempotencyKey: $idempotencyKey) { invocationId } } } }")]
    // DataFlow.Deploy has no result.
    [InlineData("mutation M($rtId: OctoObjectId!, $idempotencyKey: String) { runtime { systemCommunicationDataFlows { deploy(rtId: $rtId, idempotencyKey: $idempotencyKey) { invocationId result } } } }")]
    public async Task BrokenCommunicationDocument_IsRejected_SoTheGateIsReal(string document)
    {
        Assert.NotEmpty(await MethodClientSchemaValidationTests.DocumentErrorsAsync(Schema.Value, document));
    }

    // Answers recorded from the asset repository: the generated documents executed with the client's variables on the
    // real schema and method gateway of a tenant with System.Communication 4.19.0 (scripted handler, docs/method-clients.md).

    [Fact]
    public async Task RecordedAnswer_DeployUpdate_Succeeds()
    {
        _service.Respond(200, Recorded("adapter-deploy-update-success"));

        var result = (CkMethodResult)await CallAsync("Adapter", "DeployUpdateAsync", TargetRtId, "key-9052");

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrEmpty(result.InvocationId));
        var body = Assert.Single(_service.Bodies);
        Assert.Contains("systemCommunicationAdapters { deployUpdate(rtId: $rtId, idempotencyKey: $idempotencyKey)", body, StringComparison.Ordinal);
        Assert.Contains($"\"rtId\":\"{TargetRtId}\"", body, StringComparison.Ordinal);
        Assert.Contains("\"idempotencyKey\":\"key-9052\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RecordedAnswer_RotateServiceAccountSecret_ReturnsTheRecordWithoutASecret()
    {
        _service.Respond(200, Recorded("adapter-rotate-service-account-secret-success"));

        var result = await CallAsync("Adapter", "RotateServiceAccountSecretAsync", TargetRtId, null);

        var value = ResultValue(result);
        Assert.Equal("adapter-zz-adapter", Property(value, "ClientId"));
        Assert.Equal(true, Property(value, "RequiresPipelineRedeploy"));
        Assert.Equal(false, Property(value, "WasCreated"));
        // The rotated secret goes to the configuration, never into the answer (REST did the same, octo-communication-controller-services docs/rest-method-facades.md).
        Assert.DoesNotContain(value.GetType().GetProperties(), p => p.Name.Contains("Secret", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RecordedAnswer_SetChartVersionWithoutTheRole_Throws()
    {
        _service.Respond(200, Recorded("adapter-set-chart-version-forbidden"));

        var e = await Assert.ThrowsAsync<CkMethodCallException>(() => CallAsync("Adapter", "SetChartVersionAsync", TargetRtId, "1.4.2"));

        Assert.Equal("METHOD_FORBIDDEN", e.Code);
        Assert.Equal("System.Communication/DeployableWorkload.SetChartVersion-1", e.MethodId);
        Assert.IsAssignableFrom<QlQueryErrorException>(e);
        Assert.Contains("\"input\":{\"chartVersion\":\"1.4.2\"}", Assert.Single(_service.Bodies), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RecordedAnswer_WorkloadMethodOnTheWrongConcreteType_ThrowsTargetNotFound()
    {
        // An adapter pool's rtId sent through the Application client: the gateway looks the target up by the concrete type.
        _service.Respond(200, Recorded("application-wake-adapter-pool-target-not-found"));

        var e = await Assert.ThrowsAsync<CkMethodCallException>(() => CallAsync("Application", "WakeAsync", "6aff6ef325266ca7cb262c19"));

        Assert.Equal("METHOD_TARGET_NOT_FOUND", e.Code);
    }

    [Fact]
    public async Task RecordedAnswer_GrantLease_ReturnsTheRecord()
    {
        _service.Respond(200, Recorded("adapter-pool-grant-lease-success"));

        var result = await CallAsync("AdapterPool", "GrantLeaseAsync", "6aff6ef325266ca7cb262c19", "plant-a", TargetRtId, null, 600);

        var value = ResultValue(result);
        Assert.Equal(true, Property(value, "Granted"));
        Assert.Equal("lease-9052", Property(value, "LeaseId"));
        Assert.Contains($"\"input\":{{\"borrowerTenantId\":\"plant-a\",\"adapterRtId\":\"{TargetRtId}\",\"ttlSeconds\":600}}",
            Assert.Single(_service.Bodies), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RecordedAnswer_CancelQueuedExecution_UnknownExecutionIsATypedResult()
    {
        _service.Respond(200, Recorded("adapter-pool-cancel-queued-execution-not-found"));

        var result = (CkMethodResult)await CallAsync("AdapterPool", "CancelQueuedExecutionAsync", "6aff6ef325266ca7cb262c19",
            "6b9f2c1e-0000-4000-8000-000000009052");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsError(ErrorConstant("AdapterPool", "CancelQueuedExecution", "QueuedExecutionNotFound")));
        Assert.Equal("6b9f2c1e-0000-4000-8000-000000009052", result.Error!.Parameters["executionId"]);
    }

    [Fact]
    public async Task RecordedAnswer_Scale_CommunicationDisabledIsATypedResult()
    {
        _service.Respond(200, Recorded("adapter-pool-scale-communication-disabled"));

        var result = (CkMethodResult)await CallAsync("AdapterPool", "ScaleAsync", "6aff6ef325266ca7cb262c19", 3);

        Assert.True(result.IsError(ErrorConstant("AdapterPool", "Scale", "CommunicationDisabled")));
        Assert.Contains("\"input\":{\"replicas\":3}", Assert.Single(_service.Bodies), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RecordedAnswer_RefreshMirrors_StaticMethodReturnsTheRecord()
    {
        _service.Respond(200, Recorded("adapter-pool-refresh-mirrors-success"));

        var result = await CallAsync("AdapterPool", "RefreshMirrorsAsync");

        var value = ResultValue(result);
        Assert.Equal(2L, Convert.ToInt64(Property(value, "TenantsReconciled")));
        Assert.Equal(false, Property(value, "IsNoOp"));
        Assert.DoesNotContain("\"rtId\"", Assert.Single(_service.Bodies), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RecordedAnswer_Execute_ReturnsTheExecutionId()
    {
        _service.Respond(200, Recorded("pipeline-execute-success"));

        var result = await CallAsync("Pipeline", "ExecuteAsync", "6aff6ef325266ca7cb262c1c", "{\"meter\":\"AT001\"}", true);

        var value = ResultValue(result);
        Assert.Equal("6b9f2c1e-0000-4000-8000-00000000e001", Property(value, "ExecutionId"));
        Assert.Contains("\"isDryRun\":true", Assert.Single(_service.Bodies), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RecordedAnswer_PipelineDeploy_RejectionIsATypedResultWithTheReason()
    {
        _service.Respond(200, Recorded("pipeline-deploy-rejected"));

        var result = (CkMethodResult)await CallAsync("Pipeline", "DeployAsync", "6aff6ef325266ca7cb262c1c", TargetRtId, "triggers: []");

        Assert.True(result.IsError(ErrorConstant("Pipeline", "Deploy", "PipelineDeployRejected")));
        // reason: adapter-rejected (REST 422), adapter-service (REST 404) or unexpected (REST 400).
        Assert.Equal("adapter-rejected", result.Error!.Parameters["reason"]);
    }

    [Fact]
    public async Task RecordedAnswer_MoveToAdapter_ReturnsTheRecord()
    {
        _service.Respond(200, Recorded("pipeline-move-to-adapter-success"));

        var result = await CallAsync("Pipeline", "MoveToAdapterAsync", "6aff6ef325266ca7cb262c1c", TargetRtId, true);

        var value = ResultValue(result);
        Assert.Equal(TargetRtId, Property(value, "NewAdapterRtId"));
        Assert.Null(Property(value, "Warning"));
    }

    [Fact]
    public async Task RecordedAnswer_SetDebugging_ReturnsTheRecord()
    {
        _service.Respond(200, Recorded("pipeline-set-debugging-success"));

        var result = await CallAsync("Pipeline", "SetDebuggingAsync", "6aff6ef325266ca7cb262c1c", true);

        var value = ResultValue(result);
        Assert.Equal(true, Property(value, "Enabled"));
        Assert.Equal(false, Property(value, "AppliedToRunningAdapter"));
    }

    [Fact]
    public async Task RecordedAnswer_DataFlowDeploy_FailureIsATypedResult()
    {
        _service.Respond(200, Recorded("data-flow-deploy-failed"));

        var result = (CkMethodResult)await CallAsync("DataFlow", "DeployAsync", "6aff6ef325266ca7cb262c1b");

        Assert.True(result.IsError(ErrorConstant("DataFlow", "Deploy", "DataFlowDeployFailed")));
        Assert.Equal("unexpected", result.Error!.Parameters["reason"]);
    }

    [Fact]
    public async Task RecordedAnswer_WorkloadDeploy_ReturnsTheWarning()
    {
        _service.Respond(200, Recorded("application-deploy-warning"));

        var result = await CallAsync("Application", "DeployAsync", "6aff6ef325266ca7cb262c1a");

        var value = ResultValue(result);
        Assert.StartsWith("The service account could not be provisioned yet", (string)Property(value, "Warning")!, StringComparison.Ordinal);
        Assert.Contains("systemCommunicationApplications { deploy(", Assert.Single(_service.Bodies), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RecordedAnswer_DeploymentSiteUndeploy_FailureIsATypedResult()
    {
        _service.Respond(200, Recorded("deployment-site-undeploy-failed"));

        var result = (CkMethodResult)await CallAsync("DeploymentSite", "UndeployAsync", "6aff6ef325266ca7cb262c1f");

        Assert.True(result.IsError(ErrorConstant("DeploymentSite", "Undeploy", "DeploymentSiteUndeployFailed")));
        Assert.Equal("Deployment site 'zz-deploymentsite' is not deployed.", result.Error!.Parameters["message"]);
    }

    [Fact]
    public async Task RecordedAnswer_DeployAllTriggers_StaticMethodSucceeds()
    {
        _service.Respond(200, Recorded("pipeline-trigger-deploy-all-success"));

        var result = (CkMethodResult)await CallAsync("PipelineTrigger", "DeployAllAsync", "key-9052-triggers");

        Assert.True(result.IsSuccess);
        Assert.Contains("systemCommunicationPipelineTriggers { deployAll(idempotencyKey: $idempotencyKey)", Assert.Single(_service.Bodies), StringComparison.Ordinal);
    }

    private static List<string> ClientMethodNames(string code) =>
        Regex.Matches(code, @"public global::System\.Threading\.Tasks\.Task<[^>]+(?:<[^>]+>)?(?:<[^>]+>)?> (\w+)Async\(")
            .Select(m => m.Groups[1].Value).ToList();

    private async Task<object> CallAsync(string type, string method, params object?[] arguments)
    {
        var clientType = Generated.Value.GetType($"{Ns}.Rt{type}MethodClient")!;
        var client = Activator.CreateInstance(clientType, RealTenantClient())!;
        var info = clientType.GetMethod(method)!;
        // Trailing optional parameters (and the idempotency key) not given by the test are passed as null.
        var padded = arguments.Concat(Enumerable.Repeat<object?>(null, info.GetParameters().Length - arguments.Length)).ToArray();
        var task = (Task)info.Invoke(client, padded)!;
        await task;
        return task.GetType().GetProperty("Result")!.GetValue(task)!;
    }

    private static object ResultValue(object result)
    {
        Assert.True(((CkMethodResult)result).IsSuccess);
        return result.GetType().GetProperty("Value")!.GetValue(result)!;
    }

    private static object? Property(object value, string name) => value.GetType().GetProperty(name)!.GetValue(value);

    private static string ErrorConstant(string type, string method, string name) =>
        (string)Generated.Value.GetType($"{Ns}.Rt{type}MethodClient+{method}Errors")!.GetField(name)!.GetValue(null)!;

    private static string Recorded(string name) =>
        File.ReadAllText(Path.Combine(GeneratorFixtures.FixtureDirectory, $"assetrepo-communication-answer-{name}.json"));

    private ITenantClient RealTenantClient()
    {
        var token = A.Fake<ITenantClientAccessToken>();
        A.CallTo(() => token.AccessToken).Returns(null);
        return new TenantClient(new TenantClientOptions { EndpointUri = _service.BaseUrl, TenantId = "acme" }, token);
    }
}
