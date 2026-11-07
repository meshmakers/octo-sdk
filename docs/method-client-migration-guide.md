# Migration guide: REST clients → CK method clients (skeleton)

> **Status: skeleton (AB#8423, F5.5 AB#5703).** Each migration wave fills in its own section (first: identity wave 1,
> AB#8416). The method clients themselves are described in [method-clients.md](method-clients.md).

Phase 5 of CK v2 moves operations of `IIdentityServicesClient`, `ICommunicationServicesClient` and
`IAssetServicesClient` from REST endpoints to CK methods. For every migrated operation the REST method stays for its
REST window (one release train, at least three months from the GA of the method) and is marked `[Obsolete]` with a
pointer to its replacement. Consumers with warnings-as-errors see the obsolete warning from that release on.

## How to switch (general pattern)

1. Import the CK model that declares the method into the project that runs the source generator
   (`construction-kits.yaml`, e.g. `System.Identity-[3.0,4.0)`), so the generator emits the method client next to the DTOs.
2. Create the client over the tenant client you already use:
   `var users = new RtUserMethodClient(tenantClient);` (the tenant of `TenantClientOptions.TenantId`).
3. Replace the REST call with the method call; pass an idempotency key for calls you may retry, and reuse the same
   key for every retry of the same operation.
4. Replace exception-based handling of domain errors by the result: `if (!result.IsSuccess) { … result.Error.Code … }`
   with the constants of `{Method}Errors`. Authorization and validation errors still throw (`CkMethodCallException`,
   derived from `QlQueryErrorException`).

### Before / after

```csharp
// Before: REST
await identityServicesClient.ResetPassword(userName, newPassword);

// After: CK method client (names illustrative until the wave defines them)
var users = new RtUserMethodClient(tenantClient);
// One key per logical operation: create it once and pass the same key when you retry after a lost answer,
// so the service returns the first result instead of running the method again.
var idempotencyKey = Guid.NewGuid().ToString("N");
var result = await users.ResetPasswordAsync(userRtId, newPassword, idempotencyKey: idempotencyKey);
if (!result.IsSuccess)
{
    throw new InvalidOperationException($"Password reset refused: {result.Error!.Code}");
}
```

Differences to keep in mind:

- Methods address entities by **runtime id** (`rtId`), not by name. Look the entity up first where the REST call took a
  name.
- Errors are the declared codes of the CK model plus the platform `METHOD_*` codes; HTTP status codes are gone.
- A successful call carries an invocation id (`result.InvocationId`) for correlation with the handler log; a declared
  error carries its code and parameters instead (`result.Error`), and its `InvocationId` is `null`.

## Operation map (filled by the waves)

| Wave | REST method (`[Obsolete]` from) | CK method | Client method | Notes |
|------|----------------------------------|-----------|---------------|-------|
| Identity wave 1 (AB#8416) | _tbd_ | _tbd_ | _tbd_ | _tbd_ |
| Communication | _tbd_ | _tbd_ | _tbd_ | _tbd_ |
| Asset repository | _tbd_ | _tbd_ | _tbd_ | _tbd_ |

## Per-wave sections

### Identity wave 1 (AB#8416)

_To be written by the wave: operations, before/after per operation, error code mapping (REST status → declared code),
octo-cli commands that switched (names unchanged)._

### Further waves

_One section per wave, same structure._
