using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.BotServices;

namespace Sdk.ServiceClient.Tests.BotServices;

/// <summary>
///     AB#5543 — client methods for the secret sweep endpoints of bot-services (AB#5539). Routes are pinned
///     against a loopback HTTP listener; the report JSON is the shape the bot writes with
///     <c>SecretSweepReportJson.Options</c> (web defaults, enums by name).
/// </summary>
public class BotServicesClientSecretSweepTests : IClassFixture<LoopbackHttpService>
{
    /// <summary>Exactly what bot-services serialises for one tenant (no value, no ciphertext).</summary>
    private const string ReportJson =
        """
        {
          "tenantId": "acme",
          "mode": "CleanupUnreadable",
          "trigger": "Restore",
          "outcome": "CompletedWithFailures",
          "reason": "pre-sweep backup not required",
          "startedAt": "2026-10-06T08:00:00Z",
          "completedAt": "2026-10-06T08:00:05Z",
          "backupFileName": "acme-20261006080000.tar.gz",
          "activeKeyId": "k2",
          "strictModeActive": true,
          "strictModeViolation": true,
          "remainingLegacyValues": 3,
          "steps": [
            {
              "mode": "Verify",
              "startedAt": "2026-10-06T08:00:00Z",
              "completedAt": "2026-10-06T08:00:01Z",
              "ckTypesScanned": 2,
              "entitiesScanned": 10,
              "entitiesRewritten": 0,
              "valuesRewritten": 0,
              "placeholdersNormalized": 1,
              "skippedConcurrentlyModified": 0,
              "success": false,
              "totals": {
                "notSet": 1,
                "placeholder": 1,
                "plaintext": 2,
                "encV1": 1,
                "encV2": 4,
                "encV2ByKeyId": { "k2": 4 },
                "unknownKeyId": 1,
                "unknownKeyIdByKeyId": { "k0": 1 },
                "failed": 1,
                "total": 10,
                "legacy": 3
              },
              "slots": [
                {
                  "ckTypeId": "System.Communication/MailConnection",
                  "attributePath": "Password",
                  "counts": {
                    "notSet": 0, "placeholder": 0, "plaintext": 2, "encV1": 0, "encV2": 1,
                    "encV2ByKeyId": { "k2": 1 }, "unknownKeyId": 0, "unknownKeyIdByKeyId": {},
                    "failed": 0, "total": 3, "legacy": 2
                  }
                }
              ],
              "cleared": [
                {
                  "ckTypeId": "System.Communication/MailConnection",
                  "rtId": "65f0a1b2c3d4e5f6a7b8c9d0",
                  "attributePath": "Password",
                  "previousForm": "UnknownKeyId",
                  "keyId": "k0"
                }
              ],
              "unreadable": [
                {
                  "ckTypeId": "System.Communication/MailConnection",
                  "rtId": "65f0a1b2c3d4e5f6a7b8c9d2",
                  "attributePath": "Overrides[Key=apiToken].Value",
                  "keyId": "k9"
                }
              ],
              "failures": [
                {
                  "ckTypeId": "System.Communication/MailConnection",
                  "rtId": "65f0a1b2c3d4e5f6a7b8c9d1",
                  "attributePath": "Credentials[main].Secret",
                  "reason": "Envelope could not be parsed."
                }
              ]
            },
            {
              "mode": "CleanupUnreadable",
              "startedAt": "2026-10-06T08:00:02Z",
              "completedAt": null,
              "ckTypesScanned": 2,
              "entitiesScanned": 10,
              "entitiesRewritten": 1,
              "valuesRewritten": 1,
              "placeholdersNormalized": 0,
              "skippedConcurrentlyModified": 0,
              "skippedLegacyV1KeyMissing": 2,
              "success": true,
              "totals": {
                "notSet": 0, "placeholder": 0, "plaintext": 0, "encV1": 0, "encV2": 0,
                "encV2ByKeyId": {}, "unknownKeyId": 0, "unknownKeyIdByKeyId": {},
                "failed": 0, "total": 0, "legacy": 0
              },
              "slots": [],
              "cleared": [],
              "failures": []
            }
          ],
          "placeholdersNormalized": 1,
          "skippedLegacyV1KeyMissing": 2,
          "unreadable": [
            {
              "ckTypeId": "System.Communication/MailConnection",
              "rtId": "65f0a1b2c3d4e5f6a7b8c9d2",
              "attributePath": "Overrides[Key=apiToken].Value",
              "keyId": "k9"
            }
          ],
          "secretsToReEnter": [
            {
              "ckTypeId": "System.Communication/MailConnection",
              "rtId": "65f0a1b2c3d4e5f6a7b8c9d0",
              "attributePath": "Password",
              "previousForm": "UnknownKeyId",
              "keyId": "k0"
            }
          ]
        }
        """;

    private readonly LoopbackHttpService _service;

    public BotServicesClientSecretSweepTests(LoopbackHttpService service)
    {
        _service = service;
        _service.Reset();
    }

    private BotServicesClient CreateClient()
    {
        return new BotServicesClient(new BotServiceClientOptions { EndpointUri = _service.BaseUrl },
            A.Fake<IBotServiceClientAccessToken>());
    }

    [Fact]
    public async Task StartSecretSweepAsync_PostsToTheTenantRouteWithTheModeName()
    {
        var result = await CreateClient().StartSecretSweepAsync("acme", SecretSweepModeDto.Encrypt);

        Assert.Equal("POST /acme/v1/jobs/secret-sweep?mode=Encrypt", _service.SingleRequest());
        Assert.Equal("job-1", result.JobId);
    }

    [Fact]
    public async Task StartSecretSweepAsync_DefaultsToVerify()
    {
        await CreateClient().StartSecretSweepAsync("acme");

        Assert.Equal("POST /acme/v1/jobs/secret-sweep?mode=Verify", _service.SingleRequest());
    }

    [Fact]
    public async Task StartSecretSweepAsync_TenantIdWithPathSeparators_CannotEscapeTheTenantSegment()
    {
        await CreateClient().StartSecretSweepAsync("../system", SecretSweepModeDto.Verify);

        Assert.StartsWith("POST /..%2Fsystem/v1/jobs/secret-sweep", _service.SingleRequest());
    }

    [Fact]
    public async Task StartSecretSweepAsync_Decrypt_IsRefusedWithoutARequest()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            CreateClient().StartSecretSweepAsync("acme", SecretSweepModeDto.Decrypt));

        Assert.Empty(_service.Requests);
    }

    [Fact]
    public async Task StartSecretSweepAsync_EmptyTenantId_Throws()
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() => CreateClient().StartSecretSweepAsync(""));
    }

    [Fact]
    public async Task StartSecretSweepAllTenantsAsync_PostsToTheSystemRoute()
    {
        var result = await CreateClient().StartSecretSweepAllTenantsAsync(SecretSweepModeDto.Reprotect);

        Assert.Equal("POST /system/v1/secrets/sweep?mode=Reprotect", _service.SingleRequest());
        Assert.Equal("job-1", result.JobId);
    }

    [Fact]
    public async Task StartSecretSweepAllTenantsAsync_Confirm_SendsConfirmTrue()
    {
        await CreateClient().StartSecretSweepAllTenantsAsync(SecretSweepModeDto.CleanupUnreadable, confirm: true);

        Assert.Equal("POST /system/v1/secrets/sweep?mode=CleanupUnreadable&confirm=true",
            _service.SingleRequest());
    }

    [Fact]
    public async Task StartSecretSweepAllTenantsAsync_Decrypt_IsRefusedWithoutARequest()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            CreateClient().StartSecretSweepAllTenantsAsync(SecretSweepModeDto.Decrypt));

        Assert.Empty(_service.Requests);
    }

    [Fact]
    public async Task GetSecretSweepReportAsync_GetsTheTenantRouteAndDeserialisesTheBotShape()
    {
        _service.RespondWith("/acme/v1/jobs/secret-sweep/report", 200, ReportJson);

        var report = await CreateClient().GetSecretSweepReportAsync("acme");

        Assert.Equal("GET /acme/v1/jobs/secret-sweep/report", _service.SingleRequest());
        AssertIsTheSampleReport(report);
    }

    [Fact]
    public async Task GetSecretSweepReportAsync_NotFound_ReturnsNull()
    {
        _service.RespondWith("/acme/v1/jobs/secret-sweep/report", 404,
            """{"message":"No secret sweep report for tenant 'acme'."}""");

        var report = await CreateClient().GetSecretSweepReportAsync("acme");

        Assert.Null(report);
    }

    [Fact]
    public async Task GetSecretSweepReportAsync_OtherErrors_Throw()
    {
        _service.RespondWith("/acme/v1/jobs/secret-sweep/report", 500, """{"message":"boom"}""");

        var exception = await Assert.ThrowsAsync<ServiceClientResultException>(() =>
            CreateClient().GetSecretSweepReportAsync("acme"));
        Assert.Equal(System.Net.HttpStatusCode.InternalServerError, exception.HttpStatusCode);
    }

    [Fact]
    public async Task GetSecretSweepReportsAsync_GetsTheSystemRouteAndDeserialisesEveryTenant()
    {
        _service.RespondWith("/system/v1/secrets/reports", 200,
            "[" + ReportJson + """, { "tenantId": "beta", "mode": "Verify", "trigger": "Recurring", "outcome": "Skipped", "reason": "no keys configured", "steps": [], "secretsToReEnter": [] }]""");

        var reports = await CreateClient().GetSecretSweepReportsAsync();

        Assert.Equal("GET /system/v1/secrets/reports", _service.SingleRequest());
        Assert.Equal(2, reports.Count);
        AssertIsTheSampleReport(reports[0]);
        Assert.Equal("beta", reports[1].TenantId);
        Assert.Equal(SecretSweepTriggerDto.Recurring, reports[1].Trigger);
        Assert.Equal(SecretSweepOutcomeDto.Skipped, reports[1].Outcome);
        Assert.Null(reports[1].ActiveKeyId);
        Assert.Empty(reports[1].Steps);
    }

    [Fact]
    public async Task GetSecretSweepReportsAsync_NoReports_ReturnsEmpty()
    {
        _service.RespondWith("/system/v1/secrets/reports", 200, "[]");

        var reports = await CreateClient().GetSecretSweepReportsAsync();

        Assert.Empty(reports);
    }

    [Fact]
    public async Task StartSecretSweepAsync_Confirm_AddsConfirmTrue()
    {
        await CreateClient().StartSecretSweepAsync("acme", SecretSweepModeDto.CleanupUnreadable, confirm: true);

        Assert.Equal("POST /acme/v1/jobs/secret-sweep?mode=CleanupUnreadable&confirm=true", _service.SingleRequest());
    }

    [Fact]
    public void SecretSweepModeDto_CleanupUnreadable_KeepsTheSlotOfTheFormerClearUnknownKid()
    {
        Assert.Equal(3, (int)SecretSweepModeDto.CleanupUnreadable);
        Assert.False(Enum.IsDefined(typeof(SecretSweepModeDto), "ClearUnknownKid"));
        Assert.Equal(4, (int)SecretSweepOutcomeDto.Running);
    }

    [Fact]
    public async Task GetSecretEnvironmentStatusAsync_GetsTheTenantRouteAndDeserialises()
    {
        _service.RespondWith("/acme/v1/secrets/status", 200,
            """
            {
              "keyRingConfigured": true,
              "activeKeyId": "k1",
              "knownKeyIds": ["k1", "k0"],
              "legacyV1KeyConfigured": true,
              "strictMode": false,
              "strictModeSince": null,
              "recurringVerifyCron": "0 3 * * *",
              "lastVerifyAt": "2026-10-06T03:00:12Z",
              "warnings": []
            }
            """);

        var status = await CreateClient().GetSecretEnvironmentStatusAsync("acme");

        Assert.Equal("GET /acme/v1/secrets/status", _service.SingleRequest());
        Assert.True(status.KeyRingConfigured);
        Assert.Equal("k1", status.ActiveKeyId);
        Assert.Equal(["k1", "k0"], status.KnownKeyIds);
        Assert.True(status.LegacyV1KeyConfigured);
        Assert.False(status.StrictMode);
        Assert.Null(status.StrictModeSince);
        Assert.Equal("0 3 * * *", status.RecurringVerifyCron);
        Assert.Equal(new DateTime(2026, 10, 6, 3, 0, 12, DateTimeKind.Utc), status.LastVerifyAt!.Value.ToUniversalTime());
    }

    [Fact]
    public async Task GetSecretEnvironmentStatusAsync_NotConfigured_DeserialisesNulls()
    {
        _service.RespondWith("/acme/v1/secrets/status", 200,
            """{ "keyRingConfigured": false, "activeKeyId": null, "knownKeyIds": [], "legacyV1KeyConfigured": false, "strictMode": false, "recurringVerifyCron": null, "lastVerifyAt": null, "warnings": ["NoKeyRing"] }""");

        var status = await CreateClient().GetSecretEnvironmentStatusAsync("acme");

        Assert.False(status.KeyRingConfigured);
        Assert.Null(status.ActiveKeyId);
        Assert.Empty(status.KnownKeyIds);
        Assert.Null(status.RecurringVerifyCron);
        Assert.Null(status.LastVerifyAt);
        Assert.Equal([SecretEnvironmentWarningCodes.NoKeyRing], status.Warnings);
    }

    [Fact]
    public async Task GetSecretEnvironmentStatusAsync_WithoutWarningsField_DeserialisesAnEmptyList()
    {
        // An older bot does not send "warnings".
        _service.RespondWith("/acme/v1/secrets/status", 200, """{ "keyRingConfigured": true, "activeKeyId": "k1" }""");

        var status = await CreateClient().GetSecretEnvironmentStatusAsync("acme");

        Assert.NotNull(status.Warnings);
        Assert.Empty(status.Warnings);
    }

    [Fact]
    public void SecretEnvironmentStatus_SerialisesWarningsCamelCase()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(
            new SecretEnvironmentStatusDto { Warnings = [SecretEnvironmentWarningCodes.NoKeyRing] },
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));

        Assert.Contains("\"warnings\":[\"NoKeyRing\"]", json);
        Assert.Equal("NoLegacyV1Key", SecretEnvironmentWarningCodes.NoLegacyV1Key);
    }

    [Fact]
    public async Task GetSecretSweepRunsAsync_GetsTheTenantRouteWithLimitAndDeserialises()
    {
        _service.RespondWith("/acme/v1/secrets/sweep-runs", 200,
            """
            [
              {
                "runId": "42",
                "mode": "Encrypt",
                "trigger": "Manual",
                "outcome": "Succeeded",
                "reason": "Pre-sweep backup not taken; not required by configuration.",
                "startedAt": "2026-10-06T08:00:00Z",
                "completedAt": "2026-10-06T08:00:05Z",
                "triggeredBy": "admin",
                "totals": { "notSet": 1, "plaintext": 0, "encV1": 0, "encV2": 3, "encV2ByKeyId": { "k1": 3 }, "unknownKeyId": 1, "failed": 0, "total": 5, "legacy": 0 },
                "placeholdersNormalized": 2,
                "skippedLegacyV1KeyMissing": 3,
                "unreadableCount": 1,
                "dump": {
                  "fileName": "acme-42.presweep.tar.gz",
                  "exists": true,
                  "sizeBytes": 123456,
                  "createdAt": "2026-10-06T08:00:00Z",
                  "expiresAt": "2026-10-13T08:00:00Z",
                  "deletedAt": null,
                  "deletedBy": null
                }
              },
              {
                "runId": "43",
                "mode": "Verify",
                "trigger": "Recurring",
                "outcome": "Running",
                "startedAt": "2026-10-06T09:00:00Z",
                "completedAt": null,
                "triggeredBy": null,
                "totals": {},
                "placeholdersNormalized": 0,
                "unreadableCount": 0,
                "dump": null
              }
            ]
            """);

        var runs = await CreateClient().GetSecretSweepRunsAsync("acme", 5);

        Assert.Equal("GET /acme/v1/secrets/sweep-runs?limit=5", _service.SingleRequest());
        Assert.Equal(2, runs.Count);
        var encrypt = runs[0];
        Assert.Equal("42", encrypt.RunId);
        Assert.Equal(SecretSweepModeDto.Encrypt, encrypt.Mode);
        Assert.Equal(SecretSweepTriggerDto.Manual, encrypt.Trigger);
        Assert.Equal(SecretSweepOutcomeDto.Succeeded, encrypt.Outcome);
        Assert.Equal("admin", encrypt.TriggeredBy);
        Assert.Equal(3, encrypt.Totals.EncV2ByKeyId["k1"]);
        Assert.Equal(1, encrypt.Totals.UnknownKeyId);
        Assert.Equal(2, encrypt.PlaceholdersNormalized);
        Assert.Equal(3, encrypt.SkippedLegacyV1KeyMissing);
        Assert.Equal("Pre-sweep backup not taken; not required by configuration.", encrypt.Reason);
        Assert.Equal(1, encrypt.UnreadableCount);
        Assert.NotNull(encrypt.Dump);
        Assert.Equal("acme-42.presweep.tar.gz", encrypt.Dump!.FileName);
        Assert.True(encrypt.Dump.Exists);
        Assert.Equal(123456, encrypt.Dump.SizeBytes);
        Assert.Equal(new DateTime(2026, 10, 13, 8, 0, 0, DateTimeKind.Utc), encrypt.Dump.ExpiresAt.ToUniversalTime());
        Assert.Null(encrypt.Dump.DeletedAt);

        var running = runs[1];
        Assert.Equal(SecretSweepOutcomeDto.Running, running.Outcome);
        Assert.Null(running.Reason);
        Assert.Null(running.CompletedAt);
        Assert.Null(running.TriggeredBy);
        Assert.Null(running.Dump);
    }

    [Fact]
    public async Task GetSecretSweepRunsAsync_DefaultsToTwenty()
    {
        _service.RespondWith("/acme/v1/secrets/sweep-runs", 200, "[]");

        var runs = await CreateClient().GetSecretSweepRunsAsync("acme");

        Assert.Equal("GET /acme/v1/secrets/sweep-runs?limit=20", _service.SingleRequest());
        Assert.Empty(runs);
    }

    [Fact]
    public async Task GetSecretSweepRunsAsync_LimitBelowOne_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => CreateClient().GetSecretSweepRunsAsync("acme", 0));
        Assert.Empty(_service.Requests);
    }

    [Theory]
    [InlineData(204, SecretSweepDumpDeleteResultDto.Deleted)]
    [InlineData(404, SecretSweepDumpDeleteResultDto.NotFound)]
    [InlineData(409, SecretSweepDumpDeleteResultDto.AlreadyDeleted)]
    public async Task DeleteSecretSweepDumpAsync_DeletesTheRunDumpAndMapsTheStatus(int statusCode,
        SecretSweepDumpDeleteResultDto expected)
    {
        _service.RespondWith("/acme/v1/secrets/sweep-runs/42/dump", statusCode, "");

        var result = await CreateClient().DeleteSecretSweepDumpAsync("acme", "42");

        Assert.Equal("DELETE /acme/v1/secrets/sweep-runs/42/dump", _service.SingleRequest());
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task DeleteSecretSweepDumpAsync_Forbidden_Throws()
    {
        _service.RespondWith("/acme/v1/secrets/sweep-runs/42/dump", 403, "");

        var exception = await Assert.ThrowsAsync<ServiceClientResultException>(() =>
            CreateClient().DeleteSecretSweepDumpAsync("acme", "42"));
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, exception.HttpStatusCode);
    }

    [Fact]
    public async Task DeleteSecretSweepDumpAsync_RunIdWithPathSeparators_IsEscaped()
    {
        _service.RespondWith("/acme/v1/secrets/sweep-runs/..%2F..%2Fx/dump", 204, "");

        await CreateClient().DeleteSecretSweepDumpAsync("acme", "../../x");

        Assert.Equal("DELETE /acme/v1/secrets/sweep-runs/..%2F..%2Fx/dump", _service.SingleRequest());
    }

    [Fact]
    public async Task GetSecretEnvironmentStatusAsync_RequiredKeyIdsAndDumpKeyMissing_AreDeserialised()
    {
        // AB#5559: bot-services' status with the key ids of the encrypted dumps.
        _service.RespondWith("/acme/v1/secrets/status", 200,
            """{ "keyRingConfigured": true, "activeKeyId": "k2", "knownKeyIds": ["k2"], "warnings": ["DumpKeyMissing"], "requiredKeyIds": ["k1", "k2"] }""");

        var status = await CreateClient().GetSecretEnvironmentStatusAsync("acme");

        Assert.Equal(["k1", "k2"], status.RequiredKeyIds);
        Assert.Equal([SecretEnvironmentWarningCodes.DumpKeyMissing], status.Warnings);
    }

    [Fact]
    public async Task GetSecretEnvironmentStatusAsync_WithoutRequiredKeyIds_DeserialisesAnEmptyList()
    {
        _service.RespondWith("/acme/v1/secrets/status", 200, """{ "keyRingConfigured": true, "activeKeyId": "k1" }""");

        var status = await CreateClient().GetSecretEnvironmentStatusAsync("acme");

        Assert.NotNull(status.RequiredKeyIds);
        Assert.Empty(status.RequiredKeyIds);
    }

    [Fact]
    public void SecretEnvironmentStatus_SerialisesRequiredKeyIdsCamelCase()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(
            new SecretEnvironmentStatusDto { RequiredKeyIds = ["k1"] },
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));

        Assert.Contains("\"requiredKeyIds\":[\"k1\"]", json);
        Assert.Equal("DumpKeyMissing", SecretEnvironmentWarningCodes.DumpKeyMissing);
    }

    [Fact]
    public async Task RestoreSecretSweepDumpAsync_PostsToTheRunRouteWithConfirm()
    {
        _service.RespondWith("/acme/v1/secrets/sweep-runs/42/restore-dump", 200, """{ "jobId": "job-7" }""");

        var result = await CreateClient().RestoreSecretSweepDumpAsync("acme", "42", true);

        Assert.Equal("POST /acme/v1/secrets/sweep-runs/42/restore-dump?confirm=true", _service.SingleRequest());
        Assert.Equal("job-7", result.JobId);
    }

    [Fact]
    public async Task RestoreSecretSweepDumpAsync_WithoutConfirm_SendsNoConfirmAndMapsConfirmationRequired()
    {
        _service.RespondWith("/acme/v1/secrets/sweep-runs/42/restore-dump", 400,
            """{ "statusCode": 400, "statusDescription": "ConfirmationRequired", "message": "repeat with confirm=true" }""");

        var exception = await Assert.ThrowsAsync<SecretSweepDumpRestoreException>(() =>
            CreateClient().RestoreSecretSweepDumpAsync("acme", "42", false));

        Assert.Equal("POST /acme/v1/secrets/sweep-runs/42/restore-dump", _service.SingleRequest());
        Assert.Equal(SecretSweepDumpRestoreFailure.ConfirmationRequired, exception.Reason);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, exception.HttpStatusCode);
    }

    [Theory]
    [InlineData(404, "NotFound", SecretSweepDumpRestoreFailure.NotFound)]
    [InlineData(409, "DumpDeleted", SecretSweepDumpRestoreFailure.DumpDeleted)]
    [InlineData(409, "DumpKeyMissing", SecretSweepDumpRestoreFailure.DumpKeyMissing)]
    public async Task RestoreSecretSweepDumpAsync_Refusals_MapToTheReason(int statusCode, string code,
        SecretSweepDumpRestoreFailure expected)
    {
        // The service message names the key id; the exception must not carry anything from the body.
        _service.RespondWith("/acme/v1/secrets/sweep-runs/42/restore-dump", statusCode,
            $$"""{ "statusCode": 400, "statusDescription": "{{code}}", "message": "body-marker-kid-k9" }""");

        var exception = await Assert.ThrowsAsync<SecretSweepDumpRestoreException>(() =>
            CreateClient().RestoreSecretSweepDumpAsync("acme", "42", true));

        Assert.Equal(expected, exception.Reason);
        Assert.Equal((System.Net.HttpStatusCode)statusCode, exception.HttpStatusCode);
        Assert.DoesNotContain("body-marker", exception.Message);
    }

    [Fact]
    public async Task RestoreSecretSweepDumpAsync_DumpDeletedMessageMentioningTheOtherCode_StaysDumpDeleted()
    {
        // The run id is echoed in the message; only statusDescription decides.
        _service.RespondWith("/acme/v1/secrets/sweep-runs/DumpKeyMissing/restore-dump", 409,
            """{ "statusCode": 400, "statusDescription": "DumpDeleted", "message": "run 'DumpKeyMissing' was deleted" }""");

        var exception = await Assert.ThrowsAsync<SecretSweepDumpRestoreException>(() =>
            CreateClient().RestoreSecretSweepDumpAsync("acme", "DumpKeyMissing", true));

        Assert.Equal(SecretSweepDumpRestoreFailure.DumpDeleted, exception.Reason);
    }

    [Fact]
    public async Task RestoreSecretSweepDumpAsync_Forbidden_ThrowsTheGenericResultException()
    {
        _service.RespondWith("/acme/v1/secrets/sweep-runs/42/restore-dump", 403, "");

        var exception = await Assert.ThrowsAsync<ServiceClientResultException>(() =>
            CreateClient().RestoreSecretSweepDumpAsync("acme", "42", true));

        Assert.IsNotType<SecretSweepDumpRestoreException>(exception);
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, exception.HttpStatusCode);
    }

    [Fact]
    public async Task RestoreSecretSweepDumpAsync_RunIdWithPathSeparators_IsEscaped()
    {
        _service.RespondWith("/acme/v1/secrets/sweep-runs/..%2F..%2Fx/restore-dump", 200, """{ "jobId": "job-7" }""");

        await CreateClient().RestoreSecretSweepDumpAsync("acme", "../../x", true);

        Assert.Equal("POST /acme/v1/secrets/sweep-runs/..%2F..%2Fx/restore-dump?confirm=true", _service.SingleRequest());
    }

    [Theory]
    [InlineData("", "42")]
    [InlineData("acme", "")]
    public async Task RestoreSecretSweepDumpAsync_EmptyArguments_ThrowWithoutARequest(string tenantId, string runId)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            CreateClient().RestoreSecretSweepDumpAsync(tenantId, runId, true));
        Assert.Empty(_service.Requests);
    }

    private static void AssertIsTheSampleReport(SecretSweepReportDto? report)
    {
        Assert.NotNull(report);
        Assert.Equal("acme", report!.TenantId);
        Assert.Equal(SecretSweepModeDto.CleanupUnreadable, report.Mode);
        Assert.Equal(SecretSweepTriggerDto.Restore, report.Trigger);
        Assert.Equal(SecretSweepOutcomeDto.CompletedWithFailures, report.Outcome);
        Assert.Equal("pre-sweep backup not required", report.Reason);
        Assert.Equal(new DateTime(2026, 10, 6, 8, 0, 0, DateTimeKind.Utc), report.StartedAt.ToUniversalTime());
        Assert.Equal(new DateTime(2026, 10, 6, 8, 0, 5, DateTimeKind.Utc), report.CompletedAt.ToUniversalTime());
        Assert.Equal("acme-20261006080000.tar.gz", report.BackupFileName);
        Assert.Equal("k2", report.ActiveKeyId);
        Assert.True(report.StrictModeActive);
        Assert.True(report.StrictModeViolation);
        Assert.Equal(3, report.RemainingLegacyValues);

        Assert.Equal(2, report.Steps.Count);
        var verify = report.Steps[0];
        Assert.Equal(SecretSweepModeDto.Verify, verify.Mode);
        Assert.NotNull(verify.CompletedAt);
        Assert.Equal(2, verify.CkTypesScanned);
        Assert.Equal(10, verify.EntitiesScanned);
        Assert.Equal(1, verify.PlaceholdersNormalized);
        Assert.False(verify.Success);
        Assert.Equal(1, verify.Totals.NotSet);
        Assert.Equal(1, verify.Totals.Placeholder);
        Assert.Equal(2, verify.Totals.Plaintext);
        Assert.Equal(1, verify.Totals.EncV1);
        Assert.Equal(4, verify.Totals.EncV2);
        Assert.Equal(4, verify.Totals.EncV2ByKeyId["k2"]);
        Assert.Equal(1, verify.Totals.UnknownKeyId);
        Assert.Equal(1, verify.Totals.UnknownKeyIdByKeyId["k0"]);
        Assert.Equal(1, verify.Totals.Failed);
        Assert.Equal(10, verify.Totals.Total);
        Assert.Equal(3, verify.Totals.Legacy);

        var slot = Assert.Single(verify.Slots);
        Assert.Equal("System.Communication/MailConnection", slot.CkTypeId);
        Assert.Equal("Password", slot.AttributePath);
        Assert.Equal(2, slot.Counts.Plaintext);
        Assert.Equal(2, slot.Counts.Legacy);
        Assert.Empty(slot.Counts.UnknownKeyIdByKeyId);

        var cleared = Assert.Single(verify.Cleared);
        Assert.Equal("65f0a1b2c3d4e5f6a7b8c9d0", cleared.RtId);
        Assert.Equal(SecretValueFormDto.UnknownKeyId, cleared.PreviousForm);
        Assert.Equal("k0", cleared.KeyId);

        var stepUnreadable = Assert.Single(verify.Unreadable);
        Assert.Equal("Overrides[Key=apiToken].Value", stepUnreadable.AttributePath);
        Assert.Equal("k9", stepUnreadable.KeyId);

        var failure = Assert.Single(verify.Failures);
        Assert.Equal("Credentials[main].Secret", failure.AttributePath);
        Assert.Equal("Envelope could not be parsed.", failure.Reason);

        Assert.Equal(SecretSweepModeDto.CleanupUnreadable, report.Steps[1].Mode);
        Assert.Null(report.Steps[1].CompletedAt);

        Assert.Equal(1, report.PlaceholdersNormalized);
        Assert.Equal(2, report.SkippedLegacyV1KeyMissing);
        Assert.Equal(2, report.Steps[1].SkippedLegacyV1KeyMissing);
        Assert.Equal(0, report.Steps[0].SkippedLegacyV1KeyMissing);
        var unreadable = Assert.Single(report.Unreadable);
        Assert.Equal("System.Communication/MailConnection", unreadable.CkTypeId);
        Assert.Equal("65f0a1b2c3d4e5f6a7b8c9d2", unreadable.RtId);
        Assert.Equal("Overrides[Key=apiToken].Value", unreadable.AttributePath);
        Assert.Equal("k9", unreadable.KeyId);
        Assert.Empty(report.Steps[1].Unreadable);

        var reEnter = Assert.Single(report.SecretsToReEnter);
        Assert.Equal("System.Communication/MailConnection", reEnter.CkTypeId);
        Assert.Equal("Password", reEnter.AttributePath);
        Assert.Equal(SecretValueFormDto.UnknownKeyId, reEnter.PreviousForm);
    }
}
