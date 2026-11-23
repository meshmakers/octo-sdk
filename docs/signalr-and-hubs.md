---
description: SignalR client authentication and retry logging, the adapter hub and adapter pool hub contracts and their publish order.
applies_to: **/SignalRClient.cs, **/CommunicationControllerServices/*HubClient.cs, src/Communication.Contracts/{Hubs,MessageObjects}/**
---

# SignalR client and hub contracts

**SignalR Communication**
- **Authentication**: `SignalRClient<TOptions>.ConfigureHttpConnectionOptions` supplies the
  `IServiceClientAccessToken` the client is constructed with through
  `HttpConnectionOptions.AccessTokenProvider`. It used to write a literal
  `options.Headers["Authorization"] = "Bearer your-access-token"` under a `// TODO`, so the token
  object every caller already passed in was accepted and never read — which is why the controller's
  `/operatorHub` gate had to ship observing-only. Three reasons it is the provider and not a
  header, all load-bearing:
  - The provider is invoked on **every** connection attempt (negotiate plus each transport start), so
    the token is re-read on every reconnect. A header freezes the value at connection-object
    construction time, and the reconnect loop here reuses one `HubConnection` for days.
  - SignalR sends no headers on the WebSocket and SSE transports; the provider's value goes into the
    `access_token` query parameter there instead. The controller's `OnMessageReceived` accepts it on
    the hub paths only, for exactly this reason.
  - A blank token yields `null` ⇒ **no** credential at all, neither header nor query parameter. An
    unconfigured client must look anonymous rather than present a malformed bearer: a garbage token
    reaches the server as a *failed* authentication rather than an absent one, which reads as expired
    or revoked and hides "never given a token" from any inventory built on it.

  Tests: `tests/Sdk.ServiceClient.Tests/SignalRClientAccessTokenTests.cs` (token supplied, re-read
  rather than frozen, blank ⇒ no credential, no placeholder header, caller headers still copied, and
  the built `HubConnection` really carries the provider).
- **`ISignalRClient.IsAlive` is a state query**: it never throws and never creates a
  connection, and it is `false` for a client that is stopping, stopped or not yet started — which
  includes the gap between `StopAsync` and the next `StartAsync`. It reads the connection field
  directly. The protected `HubConnection` accessor is the opposite on purpose: it is for operations,
  throws `ObjectDisposedException` while the client is stopped and creates the connection
  on first use. Building `IsAlive` on that accessor made every poller throw during the restart a
  tenant update triggers, which ended the adapter process. Tests: `SignalRClientTests`
  (`IsAlive_AfterStop_ReturnsFalse`, `IsAlive_BeforeStart_ReturnsFalseWithoutCreatingConnection`).
- **Connect/reconnect retries log their cause**: a failed attempt in the start or reconnect
  loop is reported by `LogRetryableFailure` with exception type, message, HTTP status code (if any) and
  the innermost cause — before, the line was a bare "Common error during connect to SignalR hub X.
  Trying again.." and a hub behind a 503 ingress could not be told apart from any other failure. The
  same cause is logged at WARN at most once per `RetryFailureLogInterval` (60 s), tracked per cause;
  repeats go to Debug and are counted in the next report of that cause. Only type, message and status
  (also from a wrapped `HttpRequestException`) are logged, `access_token=`/`Bearer` values are redacted
  and the exception object is not attached. Tests: `SignalRClientRetryLoggingTests`.
- Bidirectional: Server-side `IAdapterHub` ↔ Client-side `IAdapterHubCallbacks`
- **`IAdapterPoolHub` ↔ `IAdapterPoolHubCallbacks`** — the **tenant-free** management
  channel of adapter *pool members*, served at `/adapterPoolHub` and driven client-side by
  `AdapterPoolHubClient`. 🔴 A separate hub from `IAdapterHub` on purpose:
  `/{tenantId:tenantId}/adapterHub` is tenant-addressed and `AdapterHubAuthorizationFilter`
  exists to bind a connection to its route tenant, while a pool member belongs to **no** tenant — it
  is handed one per lease. `AdapterPoolHubClient` therefore overrides `BuildServiceUri` to reach
  `{endpoint}/adapterPoolHub`, exactly as `OperatorHubClient` does for `/operatorHub`; the base
  implementation throws on a blank tenant, which is correct for every tenant-addressed hub.
  🔴 **`PoolMemberRegistrationDto.NodeDescriptors` replaced an earlier `NodeNames` string list**
  (15.09.2026), and gained a sibling `PipelineSchemaJson`. A bare name cannot answer any of
  the questions a *borrower's* `DeployPipeline` asks about the pool that will execute its pipelines —
  execution class, process-boundness, node version, configuration schema — so the field now carries
  the same `NodeDescriptorDto`s a dedicated adapter sends on `RegisterAdapterWithSchemaAsync`.
  **Source-breaking, wire-compatible:** SignalR's JSON hub protocol ignores unknown members and
  defaults absent ones, and no member ever populated `NodeNames`, so no payload on any wire carried
  it. Publish order as for every hub contract: this repo first, then `octo-communication-sdk` and
  `octo-communication-controller-services`, or they fail with `CS0117`.
  🔴 **a lease survives a controller restart.** `IAdapterPoolHub.ResumePoolMemberAsync`
  (registration with `PoolMemberRegistrationDto.ActiveLease` = `PoolMemberActiveLeaseDto`, identifiers
  only; answer carries `ActiveLeaseAdopted`) and `LeaseResultDto.ExecutionId`/`TenantId`/`MemberId`.
  All additive and defaulted. The method is separate on purpose: an older controller answers "unknown
  hub method" instead of registering a busy member as idle; the interface's default implementation
  answers `NotSupportedException` the same way, and `AdapterPoolHubClient` must implement it itself
  (pinned by `ResumePoolMember_IsImplementedByTheClient_NotInheritedFromTheInterfaceDefault`).
  Tests: `Communication.Contracts.Tests/DataTransferObjects/LeaseResumptionContractTests.cs`.
  🔴 **`LeaseDto` carries a client secret** — the borrower's own `PipelineServiceAccount` credential
  (concept §8 Q6) — so it overrides `ToString` to keep it out of any log a structured-logging
  call would produce. A record's generated `ToString` prints every property; pinned by
  `Communication.Contracts.Tests/DataTransferObjects/LeaseDtoTests.cs`.
  🟢 **`ExecutePipelineMethodInvocation` marker.** `MessageObjects/ExecutePipelineMethodInvocation`
  (`MethodId`, `InvocationId`, `CkTypeId`, `PipelineRtId`) sits next to `ExecutePipelineCaller` and is
  carried as nullable `MethodInvocation` on `ExecutePipelineRequest` and `LeaseDto` (F3.7: lets
  a `replacesGenericCreate` method's own pipeline run create entities of its type). Set **only by the
  controller's method invoker, never by clients** — the controller must overwrite/clear it on every request
  it did not build; it is a marker, not a proof (no bus-message signing). Additive and nullable: old
  payloads give `null`, old peers ignore the unknown member. Not rendered by `LeaseDto.ToString`.
  Publish order: this repo first, then communication-sdk and the controller. Tests:
  `Communication.Contracts.Tests/MessageObjects/ExecutePipelineMethodInvocationTests.cs` (System.Text.Json,
  default and web options, both directions). There is no CHANGELOG for `Communication.Contracts`; this
  section is its change record.
- Adapter lifecycle: Register → Receive config → Pre-update notifications → Send results
- `IAdapterHubCallbacks.CkModelChangedAsync(tenantId)`: controller→adapter broadcast telling
  adapters to invalidate their in-process CK model cache after a tenant update (CK model import,
  `ClearCache`). Unlike `PreUpdateTenantAsync` it does not restart the adapter. The controller sends it
  to **all** adapter connections (not routed via the adapter cache); adapters filter by their own tenant.
  Bound in `AdapterHubClient`; old adapter builds without the handler just log an unbound-method warning.
