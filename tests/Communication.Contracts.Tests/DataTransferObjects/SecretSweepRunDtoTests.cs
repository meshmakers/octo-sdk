using System.Text.Json;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

namespace Communication.Contracts.Tests.DataTransferObjects;

/// <summary>
///     AB#5534/AB#5539 — a sweep run shows the state before AND after it and what it wrote
///     (<c>GET {tenantId}/v1/secrets/sweep-runs</c>, ASP.NET camelCase).
/// </summary>
public class SecretSweepRunDtoTests
{
    private const string BotWireShape =
        """
        {
          "runId": "42",
          "mode": "Encrypt",
          "trigger": "Manual",
          "outcome": "Succeeded",
          "totals": { "plaintext": 8, "encV2": 2, "total": 10, "legacy": 8 },
          "totalsAfter": { "plaintext": 0, "encV2": 10, "total": 10, "legacy": 0 },
          "valuesRewritten": 12,
          "encryptedCount": 8,
          "skippedConcurrentlyModified": 0,
          "placeholdersNormalized": 4
        }
        """;

    [Fact]
    public void DeserialisesBeforeAfterAndWriteCounts()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

        var dto = JsonSerializer.Deserialize<SecretSweepRunDto>(BotWireShape, options);

        Assert.NotNull(dto);
        Assert.Equal(8, dto!.Totals.Plaintext);
        Assert.NotNull(dto.TotalsAfter);
        Assert.Equal(0, dto.TotalsAfter!.Plaintext);
        Assert.Equal(10, dto.TotalsAfter.EncV2);
        Assert.Equal(12, dto.ValuesRewritten);
        Assert.Equal(8, dto.EncryptedCount);
        Assert.Equal(0, dto.SkippedConcurrentlyModified);
    }

    [Fact]
    public void SerialisesTheContractNames()
    {
        var json = JsonSerializer.Serialize(new SecretSweepRunDto
        {
            TotalsAfter = new SecretFormCountsReportDto(), ValuesRewritten = 1, EncryptedCount = 1
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Contains("\"totalsAfter\":", json, StringComparison.Ordinal);
        Assert.Contains("\"valuesRewritten\":1", json, StringComparison.Ordinal);
        Assert.Contains("\"encryptedCount\":1", json, StringComparison.Ordinal);
        Assert.Contains("\"skippedConcurrentlyModified\":0", json, StringComparison.Ordinal);
    }
}
