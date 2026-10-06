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
          "mode": "ClearUnknownKid",
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
              "mode": "ClearUnknownKid",
              "startedAt": "2026-10-06T08:00:02Z",
              "completedAt": null,
              "ckTypesScanned": 2,
              "entitiesScanned": 10,
              "entitiesRewritten": 1,
              "valuesRewritten": 1,
              "placeholdersNormalized": 0,
              "skippedConcurrentlyModified": 0,
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

    private static void AssertIsTheSampleReport(SecretSweepReportDto? report)
    {
        Assert.NotNull(report);
        Assert.Equal("acme", report!.TenantId);
        Assert.Equal(SecretSweepModeDto.ClearUnknownKid, report.Mode);
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

        var failure = Assert.Single(verify.Failures);
        Assert.Equal("Credentials[main].Secret", failure.AttributePath);
        Assert.Equal("Envelope could not be parsed.", failure.Reason);

        Assert.Equal(SecretSweepModeDto.ClearUnknownKid, report.Steps[1].Mode);
        Assert.Null(report.Steps[1].CompletedAt);

        var reEnter = Assert.Single(report.SecretsToReEnter);
        Assert.Equal("System.Communication/MailConnection", reEnter.CkTypeId);
        Assert.Equal("Password", reEnter.AttributePath);
        Assert.Equal(SecretValueFormDto.UnknownKeyId, reEnter.PreviousForm);
    }
}
