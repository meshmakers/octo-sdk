---
description: Service account secret rotation and impersonation switch, the adapter pool queue and the pipeline execution failure kind.
applies_to: **/CommunicationControllerServices/{I,}CommunicationServicesClient.cs
---

# Communication services client

## Communication Services Client — `RotateServiceAccountSecretAsync`

`ICommunicationServicesClient.RotateServiceAccountSecretAsync(adapterRtId)` backs
`POST {tenantId}/v1/adapter/{adapterRtId}/serviceAccount/rotateSecret` and replaces the client
secret of the adapter's pipeline service account. Three things about it are deliberate:

- **The route takes a plain runtime object ID**, not a composite `RtEntityId` like the adapter read
  endpoints. `Adapter` is polymorphic, so the controller resolves it through the tenant's adapter
  list rather than by `RtEntityId`.
- **`RotateServiceAccountSecretResultDto` carries no secret**, mirroring the controller DTO
  one-to-one. The plaintext lives in exactly two places (the tenant's `ServiceAccountConfiguration`
  entity and the identity client's hash); returning it would add a third that ends up in proxy logs,
  shell history and CI output. `Communication.Contracts.Tests` pins that, the same way the
  controller pins it server-side — do not add a convenience property.
- **`RequiresPipelineRedeploy` must be surfaced, not swallowed.** The adapter freezes the
  credentials into the pipeline's `GlobalConfiguration` at *pipeline registration* and never
  refreshes them, so a running pipeline keeps presenting the withdrawn secret until its data flows
  are redeployed. A caller that drops the flag produces "rotation done, still broken".

A 2xx without a body throws instead of returning a default DTO: every other null-object fallback in
this client stands for an empty-but-valid state, whereas an undescribable rotation would be reported
to the user as "nothing left to do" for a secret that was in fact replaced.

Rotation cannot be done from a blueprint — the secret attribute is runtime state, so blueprint
import/export no longer touches a live secret. This client method (and `octo-cli`'s
`RotateAdapterServiceAccountSecret`) is the supported path.

## Communication Services Client — `SwitchServiceAccountsToImpersonationAsync`

`ICommunicationServicesClient.SwitchServiceAccountsToImpersonationAsync(apply, redeployDataFlows)`
backs `POST {tenantId}/v1/serviceAccount/switchToImpersonation?apply=..&redeployDataFlows=..` and
returns `ServiceAccountImpersonationSwitchReportDto` (mirror of the controller's report; no secret
or token in any member — pinned in `Communication.Contracts.Tests`). `apply=false` is a dry run.
`apply=true` clears stored secrets and invalidates the old ones, so callers confirm first; the server
answers HTTP 409 (`ServiceClientResultException.HttpStatusCode`) unless secret clearing is allowed
on the installation. A 2xx without a body throws, like the rotation. Consumer: `octo-cli`
`SwitchServiceAccountsToImpersonation`.

## Communication Services Client — the adapter pool queue

`GetAdapterPoolQueueAsync(adapterPoolRtId)` and `CancelQueuedExecutionAsync(adapterPoolRtId,
executionId)` back `GET`/`DELETE {tenantId}/v1/adapterPool/{id}/queue[/{executionId}]`. The leasing
concept requires the queue to be operable from Refinery Studio, `octo-cli` **and** the MCP
server — the plan lists those three repos, but both the CLI and the MCP server reach the controller
through this client, so the contract lives here. Three things are deliberate:

- **`AdapterPoolQueueEntryDto` has no global rank field, and adding one would be a lie.** The pool
  serves borrowing tenants round-robin, so the truthful answer is `PositionInTenant` +
  `TenantsAheadInRotation`: item 1 of the tenant whose turn is next runs before item 2 of the tenant
  being served now. `Communication.Contracts.Tests` pins the absence of a rank-shaped member the
  same way it pins "no secret" on the rotation result — it is the field a well-meaning change adds
  first, because it is the number a user asks for.
- **`ExecutionClass` is an `int`, not an enum**, for the same reason as
  `NodeDescriptorDto.ExecutionClass`: a value a reading side does not know must deserialize rather
  than throw.
- **`CancelQueuedExecutionAsync` turns `409` and `404` into outcomes, not exceptions.**
  `AdapterPoolQueueCancellationOutcome.AlreadyLeased` means the execution is already running and
  nothing was cancelled — stopping it is *interrupting a running pipeline*, a different operation
  (concept §5). Every surface has to be able to say which of the two it just did not do; an
  exception would collapse that into "the call failed, try again". Anything else still throws.

`AdapterPoolQueueEntryDto` is the **single** declaration of that wire shape: the controller
serialises this very type. Increment 7 had a controller-local copy because no client existed yet, and
increment 8 deleted it — two declarations of one shape drift, and a second one named identically also
collided inside the controller, which imports both namespaces (`CS0104`). The derived `IsLeased` is
`[JsonIgnore]`d for that reason: the controller must not gain a response field from a convenience
property. `Communication.Contracts.Tests` deserialises the controller's exact wire shape, so a silent
drift fails here rather than in a surface.

## Pipeline execution failure kind

`PipelineExecutionEndDto.FailureKind` and `LeaseResultDto.FailureKind` (`PipelineExecutionFailureKind?`: `Failed` = 0,
`OutcomeUnknown` = 1, keys equal to the CK enum `System.Communication/PipelineExecutionFailureKind`) say what a failed
execution means for its writes. The adapter (mesh adapter, SDK adapters in `octo-communication-sdk`) classifies the
exception that ended the execution with `UnacknowledgedWriteClassifier.IsOutcomeUnknown` (engine, `Runtime.Contracts`;
MongoDB write-concern errors and `UnknownTransactionCommitResult`, recognised by type name without a driver reference)
and sets the field only for `Failed`. The controller persists it on the execution and its `PipelineMethodResultMapper`
answers `METHOD_OUTCOME_UNKNOWN` + `correlationId` on `OutcomeUnknown`, exactly as the CK method host of
`octo-common-services` does for a service handler (rule R3). `null` = no failure, or an adapter/member built before the
field existed: treated as `Failed`. A structured field on purpose, never a marker in `ErrorMessage` (free text, never
parsed). Tests: `Communication.Contracts.Tests/DataTransferObjects/PipelineExecutionFailureKindContractTests`.

**On the REST pipeline execution.** `PipelineExecutionDataDto.FailureKind` (`PipelineExecutionFailureKind?`, serialized
as the enum name, `"OutcomeUnknown"` / `"Failed"`) is what `GetPipelineExecutions` / `GetLatestPipelineExecution` return, so an
operator sees whether a failed execution was an unacknowledged write. Additive: `null` for an execution that did not fail, for
an adapter that reported no kind and for interrupted or offline-buffered executions (they carry no kind and stay
`METHOD_HANDLER_FAILED` for a method caller); an older controller sends no field. Tests:
`Communication.Contracts.Tests/DataTransferObjects/PipelineExecutionDataDtoFailureKindTests`.
