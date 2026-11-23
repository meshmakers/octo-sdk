---
description: The tenant token guard and exchange, denial causes and the client-credentials request of the authenticator client.
applies_to: src/Sdk.ServiceClient/Authentication/**, tests/Sdk.ServiceClient.Tests/Authentication/**
---

# Per-tenant tokens and the authenticator client

## Per-tenant tokens — guard and exchange

A command that addresses tenant X never sends a token whose `tenant_id` claim is not X. Authority of a
root-tenant operator in a child tenant comes only from explicit provisioning there
(`ProvisionCurrentUser -ttid X`, also for new tenants); identity gives ancestor users no implicit admin role.

- **Choke point:** `AssetServicesClient.CreateTenantScopeClientAsync(tenantId)` builds every client that
  addresses a tenant by name (library status, catalog import, blueprints). It reads the token's `tenant_id`
  (`AccessTokenInspector`, no signature check, the service validates). Equal: token used as before, no exchange.
  Other tenant: exchanged through `ITenantTokenProvider`; without a provider the call fails with
  `TenantTokenMismatchException` before anything is sent. A token whose tenant cannot be read (blank, opaque)
  behaves as before. There is **no fallback** to the original token.
- **Exchange:** `AuthenticatorClient.ExchangeTokenForTenantAsync` (`ITokenExchangeClient`) posts RFC 8693
  (`acr_values=tenant:X`, client from the authenticator options, no refresh token in the answer). Same checks
  as octo-mcp-service `TenantTokenExchanger` (inputs present, successful answer, access token present) plus:
  the issued token must carry the requested tenant.
- **Cache:** `TenantTokenProvider` keeps one token per tenant in an `ITenantTokenStore` (default in memory),
  renews 60 s before expiry and when the subject token changed, silently, from the subject token.
  A host may persist the store where it already protects the subject token (octo-cli does).
- **Denial:** `TenantTokenExchangeDeniedException` carries `Cause` (`TenantTokenDenialCause`) and names the
  tenant and, for not provisioned, the step `ProvisionCurrentUser -ttid X`. `AuthenticatorClient.MapExchangeError`
  maps the OAuth errors of identity (`unauthorized_client` with "cross-tenant" = access denied, without = client
  lacks the grant; `invalid_grant` on `subject_token` = sign in again; `tenant_not_provisioned` once identity
  sends it). A 403 on a tenant scope call becomes `Cause=Forbidden` with the same hint. Messages never
  contain token content.
  An error body with an OAuth `error` is the identity service's own answer whatever the HTTP status
  (`AuthenticatorClient.MapErrorResponse`): `503 temporarily_unavailable` (a tenant database it needs is down)
  becomes `Cause=TenantUnavailable` and carries the server's `error_description`; only a response without such a body
  means `IdentityUnreachable`.
- Other clients that take the tenant in the route (`BotServicesClient` tenant jobs/secrets, communication, the typed GraphQL
  `TenantClient`) use the tenant of their options and are guarded by the host (octo-cli checks token tenant vs
  context tenant); they are not routed through the exchange.
- Tests: `Authentication/TenantTokenProviderTests`, `TenantTokenExchangeRequestTests`,
  `AssetRepositoryServices/TenantScope/TenantScopeTokenTests` (wire level, loopback).
## Authenticator Client — `RequestClientCredentialsTokenAsync`

`AuthenticatorClient.RequestClientCredentialsTokenAsync` accepts optional `clientId` / `clientSecret` parameters that, when supplied, override the values configured on `AuthenticatorOptions`. This lets callers (e.g. `octo-cli`'s non-interactive login) authenticate as a different OAuth client without rebuilding the DI graph. When `AuthenticatorOptions.TenantId` is set, the method automatically appends `acr_values=tenant:{TenantId}` to the token request — same pattern as the device and refresh flows. The identity service's `OidcTenantResolutionMiddleware` reads this to scope the per-tenant `ClientStore` lookup.
