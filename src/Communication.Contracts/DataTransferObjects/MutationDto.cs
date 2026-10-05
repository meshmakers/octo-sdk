using System.Text.Json.Serialization;
using Meshmakers.Octo.ConstructionKit.Contracts;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Represents a typed mutation.
/// </summary>
/// <typeparam name="TItemType"></typeparam>
public class MutationDto<TItemType> : MutationDto where TItemType : class
{
    /// <summary>
    ///     Item to mutate.
    /// </summary>
    public TItemType Item { get; set; } = null!;
}

/// <summary>
///     Represents a mutation.
/// </summary>
public class MutationDto
{
    /// <summary>
    ///     Runtime id of the item to mutate.
    /// </summary>
    public OctoObjectId RtId { get; set; }

    /// <summary>
    ///     Names of <c>Secret</c> attributes to clear (AB#5528, concept §4.3), GraphQL input field
    ///     <c>clearSecretAttributes: [String!]</c>. Clearing is always explicit: a secret that is
    ///     omitted from the item, or given as <c>null</c> or <c>""</c>, stays unchanged. Attribute
    ///     names are given like the item's attribute fields (camelCase in GraphQL).
    ///     Not written when <c>null</c>, so requests stay valid against servers that do not know
    ///     the field.
    /// </summary>
    [JsonPropertyName("clearSecretAttributes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Newtonsoft.Json.JsonProperty("clearSecretAttributes", NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    public IList<string>? ClearSecretAttributes { get; set; }
}
