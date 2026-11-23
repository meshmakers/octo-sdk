---
description: Tenant job routes, resumable tus upload, all-or-nothing dump download, restore outcome and stream data rollup and write calls.
applies_to: **/BotServices/**, **/AssetRepositoryServices/StreamData/**
---

# Bot services and stream data clients

## Bot Services Client — tenant job routes

`BotServicesClient` is the one tenant-addressed client **without** a `TenantId` option, deliberately.
Its five tenant-scoped job verbs — `StartDumpRepositoryAsync`, `RestoreRepositoryWithTusAsync`,
`StartExportArchiveDataAsync`, `StartImportArchiveDataWithTusAsync`, `StartRunFixupScriptAsync` —
build an absolute `{tenantId}/v1/jobs/…` URL **per call** from their own first argument
(`BuildTenantJobUri`), instead of routing through the cached base URI. Three reasons:

- **The target tenant changes per call.** Backing up a *child* tenant with the parent
  administrator's token is what the tenant routes were added for (the controller carries
  `[AllowParentTenantAdministration]`), and `octo-cli` passes the target as its own `-tid` argument.
- **`ServiceUri` is computed once and cached** (`ServiceClient.cs`), and `octo-cli` registers
  `IBotServicesClient` as a **singleton** — an options tenant would freeze the first tenant seen for
  the process lifetime, and a single-tenant unit test would not notice.
- **Public signatures stay unchanged**, so consumers migrate by taking the package, not by editing
  call sites.

What deliberately stays on `system/v1` (the client's base URI, unchanged): `GetImportJobStatus`,
both download variants and `ReconfigureLogLevelAsync` — they address a job instance or the service
process, not a tenant.

The **tus upload sink** moved onto the tenant route in stage 3 (`BuildTusUploadUri` →
`{tenantId}/v1/tus-upload`). It used to be `system/v1/tus-upload` with the tenant sent as upload
metadata, which the service's transport gate never saw and which bound nothing — the file was stored
flat under its tus file id and no consumer read the metadata back. The service now stages uploads
under the tenant's own directory. Both tus methods therefore build *two* tenant URLs in one body,
the upload and the job start; they are pinned separately in
`tests/Sdk.ServiceClient.Tests/BotServices/` because a regression to the system path would be a
silently ungated upload. `tests/Sdk.ServiceClient.Tests/BotServices/` pins all of it against
a loopback `HttpListener` (`LoopbackHttpService`) — the only way to observe the URL, since
`ServiceClient` builds its own `RestClient` and the tus flow uses a bare `HttpClient`. That fixture
is a **class fixture**: `HttpListener.Prefixes.Add` costs ~5 s per instance on macOS.

## Bot Services Client — tus upload in parts

`RestoreRepositoryWithTusAsync` and `StartImportArchiveDataWithTusAsync` (octo-cli `Restore` / `ImportArchiveData`,
and the MCP tools, which call these two methods) upload through the private `TusUploader` and never put more than
`BotServiceClientOptions.TusPartSizeBytes` into one request. The upload itself is created with BirdMessenger
(`TusCreateAsync`); the data goes out with our own `HEAD` / `PATCH` loop.

- **Why not BirdMessenger's `TusPatchAsync`:** its default upload type streams the whole file in **one** `PATCH`,
  whatever `UploadBufferSize` says. The ingress of staging-1, prod-1 and prod-2 refuses a body above 100m with 413 and
  BirdMessenger did not report it, so octo-cli failed with "Upload file for tus file ID … is empty (0 bytes)" on a
  3.48 GB dump. Do not go back to it.
- **Part size:** `TusPartSizeBytes`, default **50 MiB** (as Refinery Studio), accepted range 1 MiB to 90 MiB. A value
  outside it throws `ArgumentOutOfRangeException` when it is set (bound from configuration or assigned), because a part
  above the ingress body limit (100 MiB, 300 MiB on test-2) is refused with 413 and never reaches the service.
- **Parts and offsets:** parts are aligned to multiples of the part size (also after a resume from an odd offset), so
  the part numbers are exact and no body is larger than the part size. The first request is a `HEAD`, so an upload that
  already holds bytes continues at the server's offset. The body is streamed from the file with a known
  `Content-Length`; `Expect: 100-continue` lets an ingress answer 413 before the body is sent.
- **Failures:** a refusal (any 4xx except 408/409) throws `TusUploadException` at once with `StatusCode`, `PartNumber`,
  `PartCount`, `Offset` and `PartSizeBytes` in the exception, and no file content or response body in the message; 413
  names `TusPartSizeBytes` as the remedy. A lost connection, a timeout (30 min per request), 5xx, 408 and 409 are retried
  up to five times with a growing delay; before each retry the client reads the server's offset (`HEAD`) and continues
  from there, so nothing already stored is sent again. A connection closed while the body is sent is reported as such
  (`StatusCode` null) with the same hint, because an ingress may close instead of answering.
- **Token:** the bearer token is read from the access token object for every `HEAD` and `PATCH`, so a token renewed during a long
  upload is used from then on. A `HEAD` answered 5xx/408 while resuming counts as a failed attempt, like a lost connection.
- **Progress:** `progressCallback` receives the fraction of the file delivered, about once per MiB.
- Tests: `BotServicesClientTusUploadTests` against `TusTestServer` (loopback tus server with a request body limit,
  offset and running hash): 1 GiB through a 100 MiB limit with the defaults (21 parts, identical file), body size and
  continuous offsets for both methods, 413 with part and offset, resume after a lost answer, 503 retry, option range.

## Bot Services Client — dump download is all or nothing

`DownloadDumpToFileAsync` (used by octo-cli `Dump`, `ExportArchiveData` and the export commands) writes the body to
`<target>.<guid>.part` next to the target and moves it over the target only after the whole body arrived. A connection that
breaks off (reset, or a clean end before the announced `Content-Length`) throws an `IOException` ("The download of job '…' broke
off after N bytes: …"), deletes the part file and leaves an existing target untouched; octo-cli ends with a non-zero exit code.
Before this fix the target was opened with `FileMode.Create` and a broken download left the truncated file under its real name
(and destroyed a previous file of that name). A response without `Content-Length` (chunked) can only fail on a broken
connection; a complete chunked end cannot be told from success.

`HttpClient.Timeout` (2 h) ends with the response headers, so the body is read through `ReadIdleTimeoutStream`:
every read gets its own timer of `ServiceClientOptions.StreamReadIdleTimeout` (default 2 min, ≤ 0 = none), linked to the
caller's token. A body that delivers no data for that long (a connection that died without the client noticing) throws a
`TimeoutException` ("The download of job '…' received no data for … (read-idle timeout) …") and deletes the part file; a slow
but progressing body is never cut off, and a caller cancellation stays an `OperationCanceledException`. Tests:
`BotServicesClientDownloadTests` (raw TCP server that announces 8 MiB and sends 3 MiB then FIN, 64 KiB then RST, 64 KiB then
stalls, or a slow body in 200 ms steps; every test has a timeout) and `ReadIdleTimeoutStreamTests`.

## Bot Services Client — restore outcome in `JobDto`

`GetImportJobStatus` returns `JobDto` with optional `Outcome` (`Succeeded` | `SucceededWithWarnings`, the job status stays `Succeeded`),
`FailedArchives` (`JobFailedArchiveDto`: `ArchiveRtId`, `Reason`) and `NotCheckedRollups` (`JobNotCheckedRollupDto`: `RollupRtId`,
`Cause` NoLiveTable|NeverActivated|SourceMissing|ArchiveFailed, `Reason`, `IsBlocking`; false only for NeverActivated). Derived `HasWarnings`
is true for `SucceededWithWarnings`, a failed archive or a blocking rollup. All additive: an older bot leaves them `null`. Tests:
`BotServicesClientTests` (`GetImportJobStatus_*`).

## Stream Data Services Client — rollup validate and repair

`IStreamDataServicesClient` has four methods for the operator check after a restore:

- `ValidateRollupsAsync(tenantId, rollupRtIds?)` (`GET {tenant}/v1/streamdata/rollups/validate`, repeated `rollupRtId`, none = all
  rollups) returns `RollupConsistencyReportDto`. Read only.
- `PlanRollupRepairAsync(tenantId, rollupRtIds?)` (`POST …/rollups/repair?dryRun=true`) returns `RollupRepairReportDto`: the planned
  steps per rollup, nothing changed.
- `EnqueueRollupRepairAsync(tenantId, rollupRtIds?)` (`POST …/rollups/repair?dryRun=false`, **202**) returns `RollupRepairRequestDto`
  (`RequestId`, `State` Pending|Running|Done|Failed, `PlannedActions`, `Report` once finished). **The call runs nothing**: the server
  executes the repair in the background, so a large recompute is never bound to an HTTP request; a second request while one is active
  returns the active one.
- `GetRollupRepairRequestAsync(tenantId, requestId)` (`GET …/rollups/repair/{requestId}`) follows the request until `IsFinished`.

Check, kind, status and state are strings so a value this client does not know still deserializes.
`RollupConsistencyEntryDto` carries `IsNotChecked`, `IsBlockedByNotChecked` and `NotChecked` (`RollupNotCheckedDto`: `Cause` NoLiveTable|NeverActivated|SourceMissing, `Reason`, `IsBlocking`), `RollupConsistencyReportDto` carries `HasNotChecked` (any) and `HasBlockingNotChecked`: a rollup the validator could not check is never silently ok. A blocking gap makes `IsOk` false; `NeverActivated` is informational (`IsBlocking` false) and keeps `IsOk` true. All additive with neutral defaults, so an older server reads as `false`/`null`. Consumers: octo-cli
`ValidateRollups` / `RepairRollups` / `GetRollupRepairStatus`, MCP `validate_rollups` / `repair_rollups` / `get_rollup_repair_status`.
Tests: `RollupConsistencyClientTests` (loopback).

The deprecated `system/v1/jobs/…?tenantId=` actions are no longer addressed by the SDK at all, and
stage three of the tenant-route change removed them from the service. Do not reintroduce a call to them.

## Stream Data Services Client — writes

- `InsertTimeRangeAsync(tenantId, archiveRtId, TimeRangePointDto[])` → `POST {tenant}/v1/streamdata/archives/{id}/insertTimeRange`
  (time-range archives; a 204 of an older service counts every row as handed over).
- `InsertAsync(tenantId, archiveRtId, RawPointDto[])` → `POST {tenant}/v1/streamdata/archives/{id}/insert` (raw archives).
  Row identity: archive + `RtId` + `Timestamp` (UTC, milliseconds), protection per row; a manual point names every ingested
  column (null allowed). The endpoint is new, so any answer without a readable `StreamDataWriteResultDto` throws.
- Both send `origin` by name (`manual` / `automated`) and `reason`, never a writer (the service takes it from the token),
  send times as UTC (unspecified kind = UTC), return `StreamDataWriteResultDto` and throw on 400 / 403 / 409. An empty batch
  returns an empty result without a call. A batch with a manual point needs the tenant role `StreamDataAdmin` (403 otherwise,
  whole batch refused; asset repository); automated points need only the write scope.
  Tests: `StreamDataInsertTimeRangeClientTests`, `StreamDataInsertRawClientTests` (loopback).
