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
    [InlineData("", false)]
    // Decision 2026-10-06: a placeholder-looking input is an ordinary value, so it is set.
    [InlineData("<placeholder>", true)]
    public void Write_EmptyIsSetFalse_PlaceholderIsAValue_InBothSerializers(string raw, bool expectedIsSet)
    {
        // AB#5534: same marker as the engine wire format (RtSecretValueWireFormat.IsSet), also through
        // the untyped attribute-value converters.
        var value = RtSecretValue.Pending(raw);
        var attributes = new Dictionary<string, object?> { ["password"] = value };
        var marker = expectedIsSet ? "{\"isSet\":true}" : "{\"isSet\":false}";

        Assert.Equal(marker, JsonSerializer.Serialize(value, StjOptions));
        Assert.Equal(marker, JsonConvert.SerializeObject(value, NewtonsoftSettings));
        Assert.Equal("{\"password\":" + marker + "}", JsonSerializer.Serialize(attributes, StjOptions));
        Assert.Equal("{\"password\":" + marker + "}", JsonConvert.SerializeObject(attributes, NewtonsoftSettings));
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

    // AB#5534 round 2: the echoed read state object { isSet, keyMissing, setAt } is the marker as well.
    // The SDK converters enforce this themselves (independent of the engine version), so they are called
    // directly here; Newtonsoft would otherwise prefer the engine's type-level converter.
    [Theory]
    [InlineData("{\"isSet\":false,\"keyMissing\":true,\"setAt\":null}")]
    [InlineData("{\"isSet\":true,\"keyMissing\":false,\"setAt\":\"2026-10-06T12:34:56.789Z\"}")]
    [InlineData("{\"ISSET\":true,\"KeyMissing\":false,\"SetAt\":\"2026-10-06T12:34:56+02:00\"}")]
    [InlineData("{\"keyMissing\":true}")]
    [InlineData("{\"setAt\":\"2026-10-06\"}")]
    [InlineData("{\"isSet\":true}")]
    [InlineData("{}")]
    public void SdkConverters_ReadEchoedStateObject_IsUnchanged(string json)
    {
        Assert.Equal(RtSecretValue.Pending(string.Empty), ReadWithSdkStj(json));
        Assert.Equal(RtSecretValue.Pending(string.Empty), ReadWithSdkNewtonsoft(json, DateParseHandling.None));
        // Newtonsoft's default DateParseHandling turns the ISO string into a Date token.
        Assert.Equal(RtSecretValue.Pending(string.Empty), ReadWithSdkNewtonsoft(json, DateParseHandling.DateTime));
        Assert.Equal(RtSecretValue.Pending(string.Empty), JsonSerializer.Deserialize<RtSecretValue>(json, StjOptions));
    }

    [Theory]
    [InlineData("{\"isSet\":null}")]
    [InlineData("{\"keyMissing\":\"hunter2\"}")]
    [InlineData("{\"keyMissing\":null}")]
    [InlineData("{\"setAt\":\"hunter2\"}")]
    [InlineData("{\"setAt\":42}")]
    [InlineData("{\"setAt\":true}")]
    [InlineData("{\"setAt\":{\"value\":\"hunter2\"}}")]
    [InlineData("{\"isSet\":true,\"keyMissing\":false,\"setAt\":null,\"value\":\"hunter2\"}")]
    [InlineData("{\"isSet\":\"hunter2\"}")]
    public void SdkConverters_ReadInvalidStateObject_Throws_WithoutEchoingTheValue(string json)
    {
        var stj = Assert.Throws<JsonException>(() => ReadWithSdkStj(json));
        Assert.DoesNotContain("hunter2", stj.Message, StringComparison.Ordinal);

        var newtonsoft = Assert.ThrowsAny<Newtonsoft.Json.JsonException>(() =>
            ReadWithSdkNewtonsoft(json, DateParseHandling.DateTime));
        Assert.DoesNotContain("hunter2", newtonsoft.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SdkConverters_ReadMarkerInsideObject_LeaveTheReaderOnTheNextProperty()
    {
        const string json = "{\"a\":{\"isSet\":true,\"keyMissing\":false,\"setAt\":\"2026-10-06T12:00:00Z\"},\"b\":\"new\"}";

        var stj = JsonSerializer.Deserialize<Dictionary<string, RtSecretValue?>>(json, StjOptions)!;
        var newtonsoft = JsonConvert.DeserializeObject<NewtonsoftHolder>(json)!;

        Assert.Equal(RtSecretValue.Pending(string.Empty), stj["a"]);
        Assert.Equal(RtSecretValue.Pending("new"), stj["b"]);
        Assert.Equal(RtSecretValue.Pending(string.Empty), newtonsoft.A);
        Assert.Equal(RtSecretValue.Pending("new"), newtonsoft.B);
    }

    [Fact]
    public void SdkConverters_WriteOnlyIsSet()
    {
        var value = RtSecretValue.Protected(SecretTestValues.NewEnvelope(), DateTime.UtcNow);

        Assert.Equal("{\"isSet\":true}", JsonSerializer.Serialize(value, StjOptions));
        Assert.Equal("{\"a\":{\"isSet\":true},\"b\":null}",
            JsonConvert.SerializeObject(new NewtonsoftHolder { A = value }));
    }

    private sealed class NewtonsoftHolder
    {
        [JsonProperty("a")]
        [Newtonsoft.Json.JsonConverter(typeof(RtSecretValueNewtonsoftJsonConverter))]
        public RtSecretValue? A { get; set; }

        [JsonProperty("b")]
        [Newtonsoft.Json.JsonConverter(typeof(RtSecretValueNewtonsoftJsonConverter))]
        public RtSecretValue? B { get; set; }
    }

    private static RtSecretValue? ReadWithSdkStj(string json)
    {
        var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(json));
        reader.Read();
        return new RtSecretValueJsonConverter().Read(ref reader, typeof(RtSecretValue), StjOptions);
    }

    private static RtSecretValue? ReadWithSdkNewtonsoft(string json, DateParseHandling dateParseHandling)
    {
        using var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = dateParseHandling };
        reader.Read();
        return new RtSecretValueNewtonsoftJsonConverter().ReadJson(reader, typeof(RtSecretValue), null, false,
            Newtonsoft.Json.JsonSerializer.CreateDefault());
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

    [Fact]
    public void OctoSecretStateDto_KeyMissingAndSetAt_RoundTripAndAreOmittedWhenDefault()
    {
        var state = new OctoSecretStateDto
        {
            IsSet = false, KeyMissing = true, SetAt = new DateTime(2026, 10, 6, 8, 0, 0, DateTimeKind.Utc)
        };

        const string expected = "{\"isSet\":false,\"keyMissing\":true,\"setAt\":\"2026-10-06T08:00:00Z\"}";
        Assert.Equal(expected, JsonSerializer.Serialize(state));
        Assert.Equal(expected, JsonConvert.SerializeObject(state));

        var read = JsonSerializer.Deserialize<OctoSecretStateDto>(
            "{\"isSet\":false,\"keyMissing\":true,\"setAt\":\"2026-10-06T08:00:00Z\"}")!;
        Assert.False(read.IsSet);
        Assert.True(read.KeyMissing);
        Assert.Equal(new DateTime(2026, 10, 6, 8, 0, 0, DateTimeKind.Utc), read.SetAt!.Value.ToUniversalTime());
        Assert.Equal("{ isSet: false, keyMissing: true }", read.ToString());

        // Readable / unset secrets keep the bare marker shape that write paths accept as "unchanged".
        Assert.Equal("{\"isSet\":true}", JsonSerializer.Serialize(new OctoSecretStateDto(true)));
        Assert.Equal("{\"isSet\":true}", JsonConvert.SerializeObject(new OctoSecretStateDto(true)));
    }

    [Fact]
    public void RtEntityAttributeDto_SecretKeyMissingAndSetAt_CamelCaseAndOmittedWhenNull()
    {
        var plain = new RtEntityAttributeDto { AttributeName = "name", Value = "x" };
        Assert.DoesNotContain("secretKeyMissing", JsonSerializer.Serialize(plain));
        Assert.DoesNotContain("secretSetAt", JsonSerializer.Serialize(plain));
        Assert.DoesNotContain("secretKeyMissing", JsonConvert.SerializeObject(plain));
        Assert.DoesNotContain("secretSetAt", JsonConvert.SerializeObject(plain));

        var secret = new RtEntityAttributeDto
        {
            AttributeName = "password",
            SecretIsSet = false,
            SecretKeyMissing = true,
            SecretSetAt = new DateTime(2026, 10, 6, 8, 0, 0, DateTimeKind.Utc)
        };
        foreach (var json in new[] { JsonSerializer.Serialize(secret), JsonConvert.SerializeObject(secret) })
        {
            Assert.Contains("\"secretKeyMissing\":true", json);
            Assert.Contains("\"secretSetAt\":\"2026-10-06T08:00:00Z\"", json);
        }

        var read = JsonSerializer.Deserialize<RtEntityAttributeDto>(
            "{\"AttributeName\":\"password\",\"secretIsSet\":false,\"secretKeyMissing\":true,\"secretSetAt\":\"2026-10-06T08:00:00Z\"}")!;
        Assert.True(read.SecretKeyMissing);
        Assert.NotNull(read.SecretSetAt);
    }
}
