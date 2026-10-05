using System.Text.Json;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Runtime.Contracts.RepositoryEntities;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Communication.Contracts.Tests.Secrets;

/// <summary>
///     AB#5534: untyped attribute values (<see cref="RtEntityAttributeDto.Value" />,
///     <see cref="RtQueryCellDto.Value" />) never serialize the content of an RtSecretValue, even
///     with serializer options that do not register the secret converters.
/// </summary>
public class SecretSafeAttributeValueTests
{
    [Fact]
    public void AttributeValue_DirectSecret_DefaultOptions_IsMarker()
    {
        var dto = new RtEntityAttributeDto { AttributeName = "password", Value = SecretTestValues.NewProtected(out var envelope) };

        var stj = JsonSerializer.Serialize(dto);
        var newtonsoft = JsonConvert.SerializeObject(dto);

        Assert.Contains("\"value\":{\"isSet\":true}", stj.Replace("\"Value\"", "\"value\""), StringComparison.Ordinal);
        Assert.Contains("{\"isSet\":true}", newtonsoft, StringComparison.Ordinal);
        SecretTestValues.AssertNoSecretContent(stj, envelope);
        SecretTestValues.AssertNoSecretContent(newtonsoft, envelope);
    }

    [Fact]
    public void AttributeValue_NestedSecret_DefaultOptions_IsMarker()
    {
        var dto = new RtEntityAttributeDto
        {
            AttributeName = "list",
            Value = new List<object?>
            {
                "plain",
                SecretTestValues.NewProtected(out var first),
                new Dictionary<string, object?> { ["inner"] = SecretTestValues.NewProtected(out var second) }
            }
        };

        var stj = JsonSerializer.Serialize(dto);
        var newtonsoft = JsonConvert.SerializeObject(dto);

        SecretTestValues.AssertNoSecretContent(stj, first, second);
        SecretTestValues.AssertNoSecretContent(newtonsoft, first, second);
        Assert.Contains("\"plain\"", stj, StringComparison.Ordinal);
        Assert.Contains("\"plain\"", newtonsoft, StringComparison.Ordinal);
    }

    [Fact]
    public void AttributeValue_SecretInsideRecordObject_DefaultOptions_IsMarker()
    {
        // A raw RtRecord (not mapped to RtRecordDto) carries its secret in a nested attribute
        // dictionary; neither serializer may fall back to RtSecretValue's public members.
        var record = new RtRecord(new RtCkId<CkRecordId>("Test/Credential"), new Dictionary<string, object?>
        {
            ["user"] = "plain",
            ["password"] = SecretTestValues.NewProtected(out var envelope)
        });
        var dto = new RtEntityAttributeDto { AttributeName = "credential", Value = record };

        var stj = JsonSerializer.Serialize(dto);
        var newtonsoft = JsonConvert.SerializeObject(dto);

        SecretTestValues.AssertNoSecretContent(stj, envelope);
        SecretTestValues.AssertNoSecretContent(newtonsoft, envelope);
        Assert.Contains("\"plain\"", newtonsoft, StringComparison.Ordinal);
        Assert.Contains("{\"isSet\":true}", newtonsoft, StringComparison.Ordinal);
    }

    [Fact]
    public void QueryCellValue_Secret_IsMarker()
    {
        var cell = new RtQueryCellDto { AttributePath = "password", Value = SecretTestValues.NewProtected(out var envelope) };

        SecretTestValues.AssertNoSecretContent(JsonSerializer.Serialize(cell), envelope);
        SecretTestValues.AssertNoSecretContent(JsonConvert.SerializeObject(cell), envelope);
    }

    [Fact]
    public void AttributeValue_OrdinaryValues_SerializeAsBefore()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var dto = new RtEntityAttributeDto
        {
            AttributeName = "x",
            Value = new Dictionary<string, object?> { ["a"] = 1, ["b"] = new[] { "c" }, ["d"] = null }
        };

        var json = JsonSerializer.Serialize(dto, options);

        Assert.Equal("{\"attributeName\":\"x\",\"value\":{\"a\":1,\"b\":[\"c\"],\"d\":null}}", json);
    }

    [Fact]
    public void AttributeValue_Deserialize_StaysAJsonElement()
    {
        var dto = JsonSerializer.Deserialize<RtEntityAttributeDto>(
            "{\"AttributeName\":\"x\",\"Value\":{\"a\":1}}");

        var element = Assert.IsType<JsonElement>(dto!.Value);
        Assert.Equal(1, element.GetProperty("a").GetInt32());
        Assert.Null(dto.SecretIsSet);
    }

    [Fact]
    public void SecretIsSet_IsOmittedWhenNull_AndNamedSecretIsSet()
    {
        var plain = new RtEntityAttributeDto { AttributeName = "x", Value = "v" };
        var secret = new RtEntityAttributeDto { AttributeName = "password", SecretIsSet = true };

        Assert.DoesNotContain("secretIsSet", JsonSerializer.Serialize(plain), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secretIsSet", JsonConvert.SerializeObject(plain), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"secretIsSet\":true", JsonSerializer.Serialize(secret), StringComparison.Ordinal);
        Assert.Contains("\"secretIsSet\":true", JsonConvert.SerializeObject(secret), StringComparison.Ordinal);
        Assert.True(JsonSerializer.Deserialize<RtEntityAttributeDto>(
            "{\"AttributeName\":\"password\",\"Value\":null,\"secretIsSet\":true}")!.SecretIsSet);
    }

    [Fact]
    public void MutationDto_ClearSecretAttributes_WireNameAndOmittedWhenNull()
    {
        var withClear = new MutationDto<RtEntityDto>
        {
            RtId = OctoObjectId.GenerateNewId(),
            Item = new RtEntityDto(),
            ClearSecretAttributes = ["password", "apiKey"]
        };
        var withoutClear = new MutationDto<RtEntityDto> { RtId = OctoObjectId.GenerateNewId(), Item = new RtEntityDto() };

        Assert.Contains("\"clearSecretAttributes\":[\"password\",\"apiKey\"]", JsonSerializer.Serialize(withClear),
            StringComparison.Ordinal);
        Assert.Contains("\"clearSecretAttributes\":[\"password\",\"apiKey\"]", JsonConvert.SerializeObject(withClear),
            StringComparison.Ordinal);
        Assert.DoesNotContain("clearSecretAttributes", JsonSerializer.Serialize(withoutClear), StringComparison.Ordinal);
        Assert.DoesNotContain("clearSecretAttributes", JsonConvert.SerializeObject(withoutClear), StringComparison.Ordinal);

        var read = JsonSerializer.Deserialize<MutationDto>("{\"clearSecretAttributes\":[\"password\"]}");
        Assert.Equal(["password"], read!.ClearSecretAttributes!);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("legacy", true)]
    public void OctoSecretStateDto_IsValueSet(string? value, bool expected)
    {
        Assert.Equal(expected, OctoSecretStateDto.IsValueSet(value));
        Assert.Equal(expected, OctoSecretStateDto.FromValue(value).IsSet);
    }

    [Fact]
    public void OctoSecretStateDto_IsValueSet_AnySecretValue()
    {
        Assert.True(OctoSecretStateDto.IsValueSet(SecretTestValues.NewProtected(out _)));
        Assert.Equal("{ isSet: true }", OctoSecretStateDto.FromValue(SecretTestValues.NewProtected(out _)).ToString());
    }
}
