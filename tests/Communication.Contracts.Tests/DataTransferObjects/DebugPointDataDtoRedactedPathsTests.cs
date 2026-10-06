using System.Text.Json;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

namespace Communication.Contracts.Tests.DataTransferObjects;

/// <summary>
///     AB#5544 — debug snapshots list the JSONPaths that were redacted to <c>***</c> (handover §11), so Studio
///     marks exactly these paths. The field is optional: omitted when <c>null</c>.
/// </summary>
public class DebugPointDataDtoRedactedPathsTests
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void WritesRedactedPathsCamelCase()
    {
        var dto = new DebugPointDataDto("n1", "$.nodes[0]", null, 1)
        {
            RedactedPaths = ["$.output.smtp.password", "$.input.config.clientSecret"]
        };

        var stjJson = JsonSerializer.Serialize(dto, WebOptions);
        var newtonsoftJson = Newtonsoft.Json.JsonConvert.SerializeObject(dto);

        const string expected = "\"redactedPaths\":[\"$.output.smtp.password\",\"$.input.config.clientSecret\"]";
        Assert.Contains(expected, stjJson);
        Assert.Contains(expected, newtonsoftJson);
    }

    [Fact]
    public void OmitsRedactedPathsWhenNull()
    {
        var dto = new DebugPointDataDto("n1", "$.nodes[0]", null, 1);

        Assert.DoesNotContain("redactedPaths", JsonSerializer.Serialize(dto, WebOptions));
        Assert.DoesNotContain("redactedPaths", Newtonsoft.Json.JsonConvert.SerializeObject(dto));
    }
}
