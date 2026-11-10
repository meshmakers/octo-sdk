# CK method clients (AB#8423)

The source generator `Meshmakers.Octo.Sdk.SourceGeneration` emits a **typed method client** for every concrete CK
type that has methods (CK language 2, own, inherited or from an interface). An SDK consumer calls a CK method through
this client instead of writing GraphQL by hand. The client sends one GraphQL mutation through
`ITenantClient.SendMutationAsync` to the asset repository's typed method field (AB#5670).

Types and models **without** methods generate exactly the same sources as before; there is no option and no opt-in.
An abstract type gets no client (the asset repository has no mutation field for it either). The clients derive from
`CkMethodClientBase` in `Meshmakers.Octo.Sdk.ServiceClient`: a project that does not reference it (DTO-only, or an
older ServiceClient package without the class) gets the DTOs exactly as before and no clients, instead of code that
does not compile.

## What is generated

For a type `Account` of model `MethodClientTest` (file `…Type.AccountMethodClient.g.cs`, namespace
`{RootNamespace}.DataTransferObjects.MethodClientTest.v1`, next to the DTOs):

```csharp
public partial class RtAccountMethodClient : CkMethodClientBase
{
    public RtAccountMethodClient(ITenantClient tenantClient);

    // ResetPassword-1: instance method, String result, declared error PASSWORD_POLICY_VIOLATION
    public const string ResetPasswordMethodId = "MethodClientTest/Account.ResetPassword-1";
    public const string ResetPasswordDocument = "mutation MethodClientTestAccountResetPassword(…) { … }";
    public static class ResetPasswordErrors { public const string PasswordPolicyViolation = "PASSWORD_POLICY_VIOLATION"; }
    public Task<CkMethodResult<string?>> ResetPasswordAsync(string rtId, string newPassword, string? idempotencyKey = null);

    // ResetPassword-2: second version, optional and enum parameters, no result
    public Task<CkMethodResult> ResetPassword2Async(string rtId, string newPassword, bool? notifyUser = null,
        RtAccountStateEnum? state = null, string? idempotencyKey = null);

    // Touch-1: instance method without parameters
    public Task<CkMethodResult> TouchAsync(string rtId, string? idempotencyKey = null);

    // FindByName-1: static method (no rtId), record result
    public Task<CkMethodResult<RtAccountSummaryRecordDto?>> FindByNameAsync(string name, int? maxResults = null,
        string? idempotencyKey = null);
}
```

| CK | Client |
|----|--------|
| method `ResetPassword-2` | `ResetPassword2Async`, `ResetPassword2MethodId`, `ResetPassword2Document`, `ResetPassword2Errors` (C# name = `CkMethodIds.MethodClassName`) |
| instance method | first parameter `string rtId` |
| static method | no `rtId` |
| required parameter | non-nullable parameter, before the optional ones |
| optional parameter | nullable parameter with default `null`; not sent when `null` |
| every method | last parameter `string? idempotencyKey = null` (AB#7825) |
| result | `Task<CkMethodResult<T?>>`; a method without result returns `Task<CkMethodResult>` |
| declared errors | constants in `{Method}Errors` (`PASSWORD_POLICY_VIOLATION` → `PasswordPolicyViolation`) |
| asynchronous method (`execution.mode: async`, AB#8446) | `Task<CkMethodResult<CkOperationHandle<T?>>>`, or `Task<CkMethodResult<CkOperationHandle>>` without a result; see [Asynchronous methods](#asynchronous-methods-operation-handles-ab8446) |

Value types: `String` → `string`, `Int` → `int`, `Int64` → `long`, `Double` → `double`, `Boolean` → `bool`,
`DateTime` / `DateTimeOffset` → the .NET types, `TimeSpan` → `TimeSpan` (sent as whole seconds, the `Seconds` scalar,
`CkMethodTimeSpanSecondsConverter`), `StringArray` / `IntArray` / `Binary` → `IReadOnlyList<…>`, `Enum` → the
generated `Rt{Enum}Enum` (sent in CONSTANT_CASE by the GraphQL client), `Record` / `RecordArray` → the generated
`Rt{Record}RecordMutationDto` (parameter) or `Rt{Record}RecordDto` (result), `Secret` → `string` (parameter) or
`OctoSecretStateDto` (result). Enum and record types are referenced fully qualified in the namespace of their own model,
so an inherited method of another model resolves when that model's DTOs are generated too. A method with a
`BinaryLinked` or `GeospatialPoint` parameter or result is not generated; the client carries a
`// Unsupported by Generator: …` comment instead.

## Wire contract

The document follows the asset repository's typed method mutations (`CkMethodMutationFields`, `CkMethodGraphTypes`):

```graphql
mutation MethodClientTestAccountResetPassword2($rtId: OctoObjectId!, $input: MethodClientTestAccountResetPassword2MethodInput!, $idempotencyKey: String) {
  runtime { methodClientTestAccounts { resetPassword2(rtId: $rtId, input: $input, idempotencyKey: $idempotencyKey) { invocationId } } }
}
```

- **Type field**: GraphQL camelCase name of the concrete type plus `s` (`System.Identity/User` → `systemIdentityUsers`,
  `Named-2` → `…Named2s`).
- **Method field**: `CkMethodIds.MethodClassName` camelCase (`ResetPassword-2` → `resetPassword2`).
- **Input type**: GraphQL name of the **declaring** type + method class name + `MethodInput`; non-null when at least one
  parameter is required; absent without parameters.
- **Result**: `invocationId` and, for a method with a result, `result` (a record result selects its visible attributes,
  recursively; Hidden attributes are not in the schema). An asynchronous method selects
  `operation { operationId acceptedAtUtc retryAfterSeconds expiresAtUtc }` instead of `result` (AB#8444, AB#8446).

The naming rules live in `GraphQlNames` (`src/Sdk.SourceGeneration/MethodClientCodeGenerator.cs`) and are pinned by
test vectors shared with the asset repository (`MethodClientGenerationTests`).

## Results and errors

`CkMethodResult` (`Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants.Methods`):

- `IsSuccess`, `InvocationId` (successful calls only, for correlation with the handler log), `Value` (generic variant; the
  `CkOperationHandle` for an asynchronous method).
- `Error` (`CkMethodError`: `Code`, `Message`, `Parameters`, `Details`) and `IsError(code)` for a failure.

Only the error codes the method **declares** in its CK model become a result. Everything else throws:

| Failure | Thrown |
|---------|--------|
| another `extensions.code` (`METHOD_FORBIDDEN`, `METHOD_VALIDATION_FAILED`, `METHOD_TARGET_NOT_FOUND`, `METHOD_TIMEOUT`, `METHOD_HANDLER_FAILED`, …) | `CkMethodCallException` (`Code`, `Error`, `MethodId`); derives from `QlQueryErrorException`, so existing `catch` blocks keep working |
| GraphQL error without code (e.g. a document the schema rejects) | `QlQueryErrorException` as before |
| 401 / transport | `UnauthorizedServiceAccessException` / `ServiceClientException` as before |

`QlQueryErrorException.Errors` (new, additive) carries the GraphQL errors with their `extensions`;
`TenantClient` sets it. `CkMethodErrorReader.Read` reads the method error contract (`extensions.code`,
`extensions.parameters`, `extensions.details`) from it. `QlMutationResponseConverter` reads the single object of a
method field as the one item (create/update/delete still answer a list).

```csharp
var accounts = new RtAccountMethodClient(tenantClient);
try
{
    // Create the key once per operation and reuse it for retries of that operation.
    var result = await accounts.ResetPassword2Async(rtId, newPassword, idempotencyKey: operationKey);
    if (!result.IsSuccess && result.IsError(RtAccountMethodClient.ResetPassword2Errors.PasswordReused))
    {
        // declared error: show it to the user
    }
}
catch (CkMethodCallException e) when (e.Code == "METHOD_FORBIDDEN")
{
    // not allowed
}
```

The tenant client is the typed `TenantClient` of the tenant in its options; it is not routed through the per-tenant
token exchange (see CLAUDE.md, "Per-tenant tokens").

## Asynchronous methods: operation handles (AB#8446)

A method declared `execution.mode: async` (F3.6) does not return its result in the call. The handler accepts the call, the
platform records an **operation**, and the mutation answers with the operation handle (`operationId`, `acceptedAtUtc`,
`retryAfterSeconds`, `expiresAtUtc`). The generated client returns it as `CkOperationHandle<T>` (the declared result type) inside
the usual `CkMethodResult`:

```csharp
// System.Bot/TenantRepository.Dump-1: static, async, record result, declared errors DUMP_FAILED / DUMP_INTERRUPTED
public Task<CkMethodResult<CkOperationHandle<RtRepositoryDumpResultRecordDto?>>> DumpAsync(
    bool? includeArchiveData = null, string? idempotencyKey = null);
```

`CkOperationsClient` (same namespace, over the same `ITenantClient`) turns the handle into the result through the asset
repository's `runtime.operations` query (AB#8444):

| Member | What it does |
|--------|--------------|
| `GetOperationAsync(operationId)` | Reads one operation: `State` (`Accepted`, `Running`, `Succeeded`, `Failed`; `Unknown` for a state a newer service adds), `Progress` (`Percent`, `StageCode`, `Parameters`), `Result` (JSON), `Error` (`CkMethodError`), timestamps. Readable by the caller who started it and by tenant administrators. |
| `ListOperationsAsync(filter)` | A page of the caller's operations, newest first (`CkOperationListFilter`: `MethodId`, `State`, `Since`, `AllOwners` for tenant administrators, `Skip`, `Take` 1-200, default 50). |
| `WaitForOperationAsync(handle, progress, options, cancellationToken)` | Polls until the operation is final. `SUCCEEDED` → `CkMethodResult<T>` with the typed value; `FAILED` with a code the method declares → `CkMethodResult.Error`; every other code (`METHOD_OUTCOME_UNKNOWN` with `reason` `handler-lost`, `METHOD_HANDLER_FAILED`, …) → `CkMethodCallException`, as for a synchronous call. |
| `WaitForCompletionAsync(handle, …)` | The same poll, returns the final `CkOperation` as read without judging the error (for callers that only know the id). |

```csharp
var repository = new RtTenantRepositoryMethodClient(tenantClient);
var operations = new CkOperationsClient(tenantClient);

var accepted = await repository.DumpAsync(includeArchiveData: false);
if (!accepted.IsSuccess)
{
    // a declared error before the work started (the handler validates synchronously)
}

var handle = accepted.Value!;                           // keep handle.OperationId to read it again later
var result = await operations.WaitForOperationAsync(handle,
    new Progress<CkOperation>(o => Console.WriteLine($"{o.State} {o.Progress?.StageCode} {o.Progress?.Percent}%")),
    cancellationToken: cancellationToken);
if (result.IsSuccess)
{
    Console.WriteLine(result.Value!.DownloadPath);
}
else if (result.IsError(RtTenantRepositoryMethodClient.DumpErrors.DumpFailed))
{
    // declared error of the operation
}
```

Rules:

- **Poll interval.** The wait waits `retryAfterSeconds` of the handle before every read (`CkOperationWaitOptions`:
  `DefaultPollInterval` 2 s when the service suggests none, bounded by `MinPollInterval` 1 s and `MaxPollInterval` 30 s;
  `WaitBeforeFirstRead = false` reads a stored id at once). `BeforeReadAsync` runs before every read, e.g. to refresh the
  tenant client's access token during a long wait (octo-cli does that). `TimeProvider` is for tests.
- **Progress.** The callback gets the operation at the first read and whenever state, percentage, stage code or stage
  parameters change. Stage codes are language-neutral (`UPPER_SNAKE_CASE`); render them in the client's language.
- **Cancellation ends only the wait.** Operations have no cancel (decision AB#6927): the token stops the polling with
  `OperationCanceledException`; the operation keeps running and can be read again with its id.
- **A stored id.** `new CkOperationHandle<T>(operationId, methodId, declaredErrorCodes: …)` (or `new CkOperationHandle(operationId)`)
  recreates a handle, e.g. after a restart. Operations are kept 7 days after completion; afterwards, and for an unknown or
  another caller's id, the query answers `OPERATION_NOT_FOUND` (`CkOperationQueryException`, all three look the same).
- **Result JSON.** The operation's `result` is the JSON form of the method result; `CkOperationHandle<T>.ReadResult` reads it
  with web defaults (case-insensitive camelCase, enums as names; a `TimeSpan` result in seconds through the generated
  options). A result the CK model marks sensitive is always `null`.
- **Idempotency key.** A repeated key returns the same `operationId` (AB#8443); only one operation exists.
- **Older runtime.** The clients of asynchronous methods need the operation types of `Meshmakers.Octo.Sdk.ServiceClient`
  (`CkMethodAsyncCallResponse`, `CkOperationHandle`). A consumer whose ServiceClient package has `CkMethodClientBase` but not
  these types gets a `// Unsupported by Generator: … (asynchronous; …)` comment instead of each asynchronous method; its
  synchronous methods are generated as before.
- **Members are separate.** The operations live on `CkOperationsClient`, not on the generated client, so a CK method named
  e.g. `GetOperation` never collides with them.

## Tests

`tests/Sdk.SourceGeneration.Tests`:

- `MethodClientGenerationTests` — clients per type, signatures, documents, inherited methods, declared error
  constants, naming vectors shared with the asset repository, abstract types, generated code compiles without warnings.
- `MethodClientSchemaValidationTests` — every document generated for the asset repository's integration-test model
  validates against the recorded schema of that tenant (GraphQL.NET `DocumentValidator`, core rules); broken documents are
  rejected.
- `MethodClientRuntimeTests` — compiled clients: variables on the wire (enum CONSTANT_CASE, omitted optional
  parameters), declared error as result, undeclared and authorization errors thrown, real `TenantClient` against a
  loopback endpoint, including answers recorded from the asset repository (`assetrepo-answer-*.json`).
- `UserMethodClientTests` (AB#8416) — the `RtUserMethodClient` a consumer generates from System.Identity 3.2.0:
  signatures of the identity wave 1 operations, declared error constants, generated code compiles without warnings,
  every System.Identity document (User, Group, Client, DataPermission) validates against the recorded schema of a tenant
  with System.Identity 3.2.0, and answers recorded from the asset repository's method gateway come back as typed results
  (`CreateUser` result, declared errors of `ResetPassword` and `AddRole`, `METHOD_FORBIDDEN` thrown, `Merge` success).
  System.Identity is generator input of the tests only; the SDK ships no System.Identity surface.
- `AsyncMethodClientTests` (AB#8446) — the `RtTenantRepositoryMethodClient` generated from System.Bot 3.7.0: signature
  with `CkOperationHandle<T>`, the document selects `operation` (validated against the recorded schema of a test-2 tenant
  with System.Bot 3.7.0; the former document with `result` is rejected), the recorded accepted answer becomes the handle,
  `WaitForOperationAsync` over recorded `RUNNING` / `SUCCEEDED` answers (typed result, progress reports, `retryAfterSeconds`
  as delay and its bounds), declared error as result, `METHOD_OUTCOME_UNKNOWN` thrown, cancellation sends nothing more,
  `OPERATION_NOT_FOUND`, the list filter on the wire. The operations documents validate against the same schema.
- `SyncMethodClientGoldenTests` (AB#8446) — snapshot gate: every `*MethodClient.g.cs` of the three synchronous fixture models
  is byte-identical to the output of the generator before AB#8446 (`Fixtures/Golden/*.methodclients.golden.txt`).
- `MethodlessGeneratedCodeGoldenTests` — snapshot gate: all non-client sources of three real models are byte-identical
  to the output of the generator before AB#8423 (`Fixtures/Golden`).

`tests/Sdk.ServiceClient.Tests/AssetRepositoryServices/Tenants/Methods/CkMethodRuntimeSupportTests` — response converter
and error reader.

### Fixtures and how to record them again

All fixtures are real build output; never edit them by hand.

| Fixture | Source | Recording |
|---------|--------|-----------|
| `ck-methodclienttest.yaml` / `.cache.json` | `Fixtures/MethodClientModel/ConstructionKit` | compile the folder with `Meshmakers.Octo.ConstructionKit.MsBuildTasks` (`OctoCompileCkModel=true`, `OctoPublishCkModel=false`, a project with `<ConstructionKitFolder Include="…/MethodClientModel/ConstructionKit" />`); copy `bin/…/octo-ck-libraries/<project>/out/ck-methodclienttest.yaml` and `obj/…/octo-ck-cache/<project>/out/ck-methodclienttest.cache.json` |
| `ck-assetrepositoryintegrationtest.yaml` / `.cache.json` | octo-asset-repo-services `tests/AssetRepositoryIntegrationTestCkModel/ConstructionKit` | same, pointing the `ConstructionKitFolder` at that folder |
| `assetrepo-integrationtest-schema.graphql.gz` | SDL of the asset repository's integration-test tenant, built by the real schema generation against a local MongoDB | in octo-asset-repo-services (DebugL chain built locally, local infrastructure running): `dotnet test tests/AssetRepositoryServices.IntegrationTests -c DebugL -e OCTO_SCHEMA_EXPORT_PATH=/tmp/schema.graphql --filter "FullyQualifiedName~SchemaExportTests"`, then `gzip -9 -c /tmp/schema.graphql > Fixtures/assetrepo-integrationtest-schema.graphql.gz`. Pass the path with `-e`; an exported shell variable does not reach the test host |
| `assetrepo-answer-*.json` | answers of the asset repository to generated documents of `AccessTestAccount` (declared error, `METHOD_FORBIDDEN`, success), executed with the client's variables on the real schema and method gateway (`MethodGatewayGraphQlTestFixture`, scripted handler) | a throw-away test in the octo-asset-repo-services integration tests (collection `MethodGateway`) that runs the `…Document` constants of the generated `RtAccessTestAccountMethodClient` through `ExecuteGraphQlAsync(document, variablesJson, user)` and writes `SerializeGraphQl(result)`; not committed there |
| `ck-system.identity-3.yaml` / `.cache.json` | released System.Identity 3.2.0 from the CK catalog (AB#8416) | a scratch project with `Meshmakers.Octo.ConstructionKit.MsBuildTasks` (`OctoCompileCkModel=false`, `OctoPublishCkModel=false`) and a `construction-kits.yaml` importing `System.Identity-[3.2,4.0)`; build it and copy the restored `ck-system.identity-3.yaml` and `ck-system.identity-3.cache.json` |
| `assetrepo-system-identity-schema.graphql.gz` | SDL of an asset repository tenant with System.Identity 3.2.0 imported (`IdentityCredentialGuardFixture`: integration-test tenant plus the embedded System.Identity snapshot) | in octo-asset-repo-services: put the `ck-system.identity-3.yaml` above as `testData/ck-models/ck-system.identity-3.2.0.yaml` and point `SystemIdentitySnapshotCkModel` at it, then a throw-away test in collection `IdentityCredentialGuard` writes `ISchemaContext.GetOrCreateAsync(tenant).Print()` (as `SchemaExportTests` does); gzip it as above. Not committed there |
| `assetrepo-identity-answer-*.json` | answers of the asset repository to generated `RtUserMethodClient` documents (CreateUser success with result, ResetPassword and AddRole declared errors, ResetPassword without `UserManagement` → `METHOD_FORBIDDEN`, Merge success) on the same fixture, scripted handler | the same throw-away test runs the `…Document` constants through `ExecuteGraphQlAsync(document, variablesJson, user)` and writes `SerializeGraphQl(result)` |
| `ck-system.bot-3.yaml` / `.cache.json` | System.Bot 3.7.0 from octo-bot-services `src/SystemBotCkModel/ConstructionKit` (origin/main 8e951fd), AB#8446 | a scratch project with `Meshmakers.Octo.ConstructionKit.MsBuildTasks` and `Meshmakers.Octo.ConstructionKit.Models.System` (`OctoCompileCkModel=true`, `OctoPublishCkModel=false`, `<ConstructionKitFolder Include="…/SystemBotCkModel/ConstructionKit" />`); copy `bin/…/octo-ck-libraries/<project>/out/ck-system.bot-3.yaml` and `obj/…/octo-ck-cache/<project>/out/ck-system.bot-3.cache.json` |
| `assetrepo-system-bot-schema.graphql.gz` | SDL of a test-2 scratch tenant with System.Bot 3.7.0 (asset repository with AB#8444) | `octo-cli -c GraphQl -f <introspection query>` against the scratch tenant (standard introspection query of `graphql` 16, `getIntrospectionQuery`), then `printSchema(buildClientSchema(…))` with `graphql` 16 in Node; gzip as above |
| `assetrepo-bot-answer-*.json` | answers of the test-2 asset repository on the same scratch tenant: the dump mutation (`dump-accepted`), `runtime.operations` by id while `RUNNING` and after `SUCCEEDED`, an unknown id (`operation-not-found`) and the list with `methodId` / `state` (`operation-list`) | `octo-cli -c GraphQl` with the generated `DumpDocument` and `CkOperationsClient.GetOperationDocument` / `ListOperationsDocument`; the not-found body is the one `GraphQl` writes to standard error |
| `Golden/*.methodclients.golden.txt` | method clients of the synchronous fixtures before AB#8446 (origin/main 7c48ed3) | re-record only for an intended change of the method client generator: concatenate every `*MethodClient.g.cs` in ordinal hint order as `//// {hint}\n{code}` |
| `Golden/*.golden.txt` | generator output before AB#8423 (origin/main a02fdf2) | re-record only for an intended change of the DTO generators: concatenate every non-client source of the fixture in ordinal hint order as `//// {hint}\n{code}` |

Recorded for AB#8423 from octo-asset-repo-services 603d54f (2026-11-07). Re-record the schema and the
`ck-assetrepositoryintegrationtest` fixture together when the asset repository changes its method fields.
The System.Identity schema and answers were recorded for AB#8416 from octo-asset-repo-services 603d54f with
System.Identity 3.2.0 (2026-11-07); re-record them when System.Identity's User methods or the method fields change.
The System.Bot schema and answers were recorded for AB#8446 on test-2 (asset repository with AB#8444, bot services with AB#8445,
scratch tenant `tmp-d8446-ops`, 2026-11-10); re-record them when the operation handle, `runtime.operations` or
`TenantRepository.Dump-1` change.
