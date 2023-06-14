using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;

/// <summary>
///     Represents the result of a mutation response
/// </summary>
/// <typeparam name="TDto"></typeparam>
public class QlMutationResponse<TDto>
{
    // ReSharper disable once CollectionNeverUpdated.Local
    [JsonExtensionData] private readonly IDictionary<string, JToken> _additionalData;

    /// <summary>
    ///     Constructor
    /// </summary>
    public QlMutationResponse()
    {
        _additionalData = new Dictionary<string, JToken>();
    }

    // ReSharper disable once MemberCanBePrivate.Global
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public TDto? Result { get; private set; }


    [OnDeserialized]
    // ReSharper disable once UnusedMember.Global
    // ReSharper disable once UnusedParameter.Global
    internal void OnDeserializedMethod(StreamingContext context)
    {
        Result = _additionalData.Values.First().ToObject<TDto>();
    }
}
