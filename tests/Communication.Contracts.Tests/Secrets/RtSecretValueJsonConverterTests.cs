using System.Text.Json;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Contracts.Serialization;
using Meshmakers.Octo.Runtime.Contracts.RepositoryEntities;
using Newtonsoft.Json;
using JsonException = System.Text.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Communication.Contracts.Tests.Secrets;

/// <summary>
///     AB#5534: the RtSecretValue converters write the marker <c>{"isSet":true}</c> only and read a
///     string as pending input, the marker as "unchanged" (<c>Pending("")</c>).
/// </summary>
public class RtSecretValueJsonConverterTests
{
    private static readonly JsonSerializerOptions StjOptions = new JsonSerializerOptions().AddOctoSecretConverters();
    private static readonly JsonSerializerSettings NewtonsoftSettings = new JsonSerializerSettings().AddOctoSecretConverters();

    public static TheoryData<string> SecretKinds => new() { "protected", "legacyPlaintext", "legacyV1", "pending" };

    private static RtSecretValue Create(string kind, out string content)
    {
        switch (kind)
        {
            case "protected":
                return SecretTestValues.NewProtected(out content);
            case "legacyPlaintext":
                content = SecretTestValues.FakePlaintext;
                return RtSecretValue.LegacyPlaintext(content);
            case "legacyV1":
                content = SecretTestValues.NewLegacyV1Value();
                return RtSecretValue.LegacyPlaintext(content);
            default:
                content = SecretTestValues.FakePlaintext;
                return RtSecretValue.Pending(content);
        }
    }

    [Theory]
    [MemberData(nameof(SecretKinds))]
    public void Stj_Write_IsMarkerOnly(string kind)
    {
        var value = Create(kind, out var content);

        var json = JsonSerializer.Serialize(value, StjOptions);

        Assert.Equal("{\"isSet\":true}", json);
        SecretTestValues.AssertNoSecretContent(json, content);
    }

    [Theory]
    [MemberData(nameof(SecretKinds))]
    public void Newtonsoft_Write_IsMarkerOnly(string kind)
    {
        var value = Create(kind, out var content);

        var json = JsonConvert.SerializeObject(value, NewtonsoftSettings);

        Assert.Equal("{\"isSet\":true}", json);
        SecretTestValues.AssertNoSecretContent(json, content);
    }

    [Fact]
    public void Stj_WriteInsideUntypedDictionary_UsesTheConverterByRuntimeType()
    {
        var value = SecretTestValues.NewProtected(out var envelope);
        var attributes = new Dictionary<string, object?> { ["password"] = value, ["name"] = "x" };

        var json = JsonSerializer.Serialize(attributes, StjOptions);

        Assert.Equal("{\"password\":{\"isSet\":true},\"name\":\"x\"}", json);
        SecretTestValues.AssertNoSecretContent(json, envelope);
    }

    [Fact]
    public void Newtonsoft_WriteInsideUntypedDictionary_UsesTheConverter()
    {
        var value = SecretTestValues.NewProtected(out var envelope);
        var attributes = new Dictionary<string, object?> { ["password"] = value };

        var json = JsonConvert.SerializeObject(attributes, NewtonsoftSettings);

        Assert.Equal("{\"password\":{\"isSet\":true}}", json);
        SecretTestValues.AssertNoSecretContent(json, envelope);
    }

    [Fact]
    public void Stj_ReadString_IsPendingInput()
    {
        var value = JsonSerializer.Deserialize<RtSecretValue>($"\"{SecretTestValues.FakePlaintext}\"", StjOptions);

        Assert.NotNull(value);
        Assert.True(value.IsPending);
        Assert.Equal(RtSecretValue.Pending(SecretTestValues.FakePlaintext), value);
    }

    [Fact]
    public void Newtonsoft_ReadString_IsPendingInput()
    {
        var value = JsonConvert.DeserializeObject<RtSecretValue>($"\"{SecretTestValues.FakePlaintext}\"", NewtonsoftSettings);

        Assert.NotNull(value);
        Assert.True(value.IsPending);
        Assert.Equal(RtSecretValue.Pending(SecretTestValues.FakePlaintext), value);
    }

    [Theory]
    [InlineData("{\"isSet\":true}")]
    [InlineData("{\"isSet\":false}")]
    [InlineData("{\"IsSet\":true}")]
    [InlineData("{}")]
    [InlineData("\"\"")]
    public void Stj_ReadMarkerOrEmpty_IsUnchanged(string json)
    {
        var value = JsonSerializer.Deserialize<RtSecretValue>(json, StjOptions);

        Assert.Equal(RtSecretValue.Pending(string.Empty), value);
    }

    [Theory]
    [InlineData("{\"isSet\":true}")]
    [InlineData("{\"isSet\":false}")]
    [InlineData("{}")]
    [InlineData("\"\"")]
    public void Newtonsoft_ReadMarkerOrEmpty_IsUnchanged(string json)
    {
        var value = JsonConvert.DeserializeObject<RtSecretValue>(json, NewtonsoftSettings);

        Assert.Equal(RtSecretValue.Pending(string.Empty), value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("<placeholder>")]
    public void Write_EmptyOrPlaceholder_IsSetFalse_InBothSerializers(string raw)
    {
        // AB#5534: same marker as the engine wire format (RtSecretValueWireFormat.IsSet), also through
        // the untyped attribute-value converters.
        var value = RtSecretValue.Pending(raw);
        var attributes = new Dictionary<string, object?> { ["password"] = value };

        Assert.Equal("{\"isSet\":false}", JsonSerializer.Serialize(value, StjOptions));
        Assert.Equal("{\"isSet\":false}", JsonConvert.SerializeObject(value, NewtonsoftSettings));
        Assert.Equal("{\"password\":{\"isSet\":false}}", JsonSerializer.Serialize(attributes, StjOptions));
        Assert.Equal("{\"password\":{\"isSet\":false}}", JsonConvert.SerializeObject(attributes, NewtonsoftSettings));
    }

    [Fact]
    public void ReadNull_StaysNull()
    {
        Assert.Null(JsonSerializer.Deserialize<RtSecretValue>("null", StjOptions));
        Assert.Null(JsonConvert.DeserializeObject<RtSecretValue>("null", NewtonsoftSettings));
    }

    [Theory]
    [InlineData("42")]
    [InlineData("true")]
    [InlineData("[\"x\"]")]
    [InlineData("{\"envelope\":\"enc:v2:k1:AAAA\"}")]
    [InlineData("{\"isSet\":\"yes\"}")]
    public void ReadAnythingElse_Throws_WithoutEchoingTheValue(string json)
    {
        var stj = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<RtSecretValue>(json, StjOptions));
        Assert.DoesNotContain("enc:", stj.Message, StringComparison.Ordinal);

        var newtonsoft = Assert.ThrowsAny<Newtonsoft.Json.JsonException>(() =>
            JsonConvert.DeserializeObject<RtSecretValue>(json, NewtonsoftSettings));
        Assert.DoesNotContain("enc:", newtonsoft.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RoundTrip_ReadBackMarker_KeepsTheStoredSecret()
    {
        // A document that was read (marker) and is written back must not clear the secret:
        // Pending("") is "unchanged" in the engine write rules, null would clear.
        var json = JsonSerializer.Serialize(SecretTestValues.NewProtected(out _), StjOptions);

        var readBack = JsonSerializer.Deserialize<RtSecretValue>(json, StjOptions);

        Assert.NotNull(readBack);
        Assert.Equal(RtSecretValue.Pending(string.Empty), readBack);
    }

    [Fact]
    public void AddOctoSecretConverters_IsIdempotent()
    {
        var options = new JsonSerializerOptions().AddOctoSecretConverters().AddOctoSecretConverters();
        var settings = new JsonSerializerSettings().AddOctoSecretConverters().AddOctoSecretConverters();

        Assert.Single(options.Converters.OfType<RtSecretValueJsonConverter>());
        Assert.Single(settings.Converters.OfType<RtSecretValueNewtonsoftJsonConverter>());
    }

    [Fact]
    public void OctoSecretStateDto_HasTheMarkerShape()
    {
        Assert.Equal("{\"isSet\":true}", JsonSerializer.Serialize(new OctoSecretStateDto(true)));
        Assert.Equal("{\"isSet\":false}", JsonConvert.SerializeObject(new OctoSecretStateDto(false)));
        Assert.True(JsonSerializer.Deserialize<OctoSecretStateDto>("{\"isSet\":true}")!.IsSet);
    }
}
