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
  recursively; Hidden attributes are not in the schema).

The naming rules live in `GraphQlNames` (`src/Sdk.SourceGeneration/MethodClientCodeGenerator.cs`) and are pinned by
test vectors shared with the asset repository (`MethodClientGenerationTests`).

## Results and errors

`CkMethodResult` (`Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants.Methods`):

- `IsSuccess`, `InvocationId` (successful calls only, for correlation with the handler log), `Value` (generic variant; `default` for an
  accepted asynchronous call whose result is not available yet).
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
| `Golden/*.golden.txt` | generator output before AB#8423 (origin/main a02fdf2) | re-record only for an intended change of the DTO generators: concatenate every non-client source of the fixture in ordinal hint order as `//// {hint}\n{code}` |

Recorded for AB#8423 from octo-asset-repo-services 603d54f (2026-11-07). Re-record the schema and the
`ck-assetrepositoryintegrationtest` fixture together when the asset repository changes its method fields.
