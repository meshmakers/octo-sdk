using System.Text.Json;
using GraphQL;
using GraphQL.Client.Serializer.SystemTextJson;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants.Methods;

namespace Sdk.ServiceClient.Tests.AssetRepositoryServices.Tenants.Methods;

/// <summary>
///     AB#8423: the runtime pieces under the generated method clients — the mutation response converter reads the
///     single object of a method field (and still the list of create/update), and the method error contract is read
///     from both forms the GraphQL client produces.
/// </summary>
public class CkMethodRuntimeSupportTests
{
    private static JsonSerializerOptions Options()
    {
        var options = SystemTextJsonSerializer.DefaultJsonSerializerOptions;
        options.Converters.Add(new QlMutationResponseConverterFactory());
        options.PropertyNameCaseInsensitive = true;
        return options;
    }

    public sealed class Item
    {
        public string? InvocationId { get; set; }
        public string? Name { get; set; }
    }

    [Fact]
    public void MutationResponse_MethodField_SingleObjectBecomesTheOnlyItem()
    {
        var response = JsonSerializer.Deserialize<QlMutationResponse<Item>>(
            """{"runtime":{"systemIdentityUsers":{"resetPassword":{"invocationId":"i-1"}}}}""", Options());

        Assert.Equal("i-1", Assert.Single(response!.Items!).InvocationId);
    }

    [Fact]
    public void MutationResponse_CreateField_ListIsReadAsBefore()
    {
        var response = JsonSerializer.Deserialize<QlMutationResponse<Item>>(
            """{"runtime":{"systemIdentityUsers":{"create":[{"name":"a"},{"name":"b"}]}}}""", Options());

        Assert.Equal(["a", "b"], response!.Items!.Select(i => i.Name));
    }

    [Fact]
    public void MutationResponse_FailedMethodField_IsNull()
    {
        var response = JsonSerializer.Deserialize<QlMutationResponse<Item>>(
            """{"runtime":{"systemIdentityUsers":{"resetPassword":null}}}""", Options());

        Assert.Null(response);
    }

    [Fact]
    public void ErrorReader_ReadsMapForm()
    {
        var error = CkMethodErrorReader.Read([
            new GraphQLError { Message = "no code" },
            new GraphQLError
            {
                Message = "The CK method call failed: X_FAILED",
                Extensions = new Map
                {
                    ["code"] = "X_FAILED",
                    ["parameters"] = new Map { ["count"] = 3L, ["flag"] = true },
                    ["details"] = new List<object?> { new Map { ["code"] = "D", ["parameters"] = new Map { ["f"] = "v" } } }
                }
            }
        ]);

        Assert.NotNull(error);
        Assert.Equal("X_FAILED", error.Code);
        Assert.Equal("3", error.Parameters["count"]);
        Assert.Equal("true", error.Parameters["flag"]);
        Assert.Equal("v", Assert.Single(error.Details).Parameters["f"]);
    }

    [Fact]
    public void ErrorReader_ReadsJsonElementForm()
    {
        using var document = JsonDocument.Parse(
            """{"code":"X_FAILED","parameters":{"methodId":"A/B.C-1"},"details":[{"code":"D","parameters":{}}]}""");
        var extensions = new Map();
        foreach (var property in document.RootElement.EnumerateObject())
        {
            extensions[property.Name] = property.Value.Clone();
        }

        var error = CkMethodErrorReader.Read([new GraphQLError { Message = "m", Extensions = extensions }]);

        Assert.Equal("X_FAILED", error!.Code);
        Assert.Equal("A/B.C-1", error.Parameters["methodId"]);
        Assert.Equal("D", Assert.Single(error.Details).Code);
    }

    [Fact]
    public void ErrorReader_NoCode_IsNull()
    {
        Assert.Null(CkMethodErrorReader.Read(null));
        Assert.Null(CkMethodErrorReader.Read([new GraphQLError { Message = "Field 'x' not found" }]));
    }

    [Fact]
    public void QlQueryErrorException_CarriesTheErrors()
    {
        var errors = new[] { new GraphQLError { Message = "m" } };

        Assert.Same(errors, new QlQueryErrorException("m", errors).Errors);
        Assert.Null(new QlQueryErrorException("m").Errors);
    }
}
