using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Frontend.Client.Tenants;

/// <summary>
///     Represents an Octo query response
/// </summary>
/// <typeparam name="TDto"></typeparam>
// ReSharper disable once ClassNeverInstantiated.Global
public class QlQueryResponse<TDto> where TDto : class
{
    // ReSharper disable once CollectionNeverUpdated.Local
    [JsonExtensionData] private readonly IDictionary<string, JToken> _additionalData;

    /// <summary>
    ///     Constructor
    /// </summary>
    public QlQueryResponse()
    {
        _additionalData = new Dictionary<string, JToken>();
    }


    // ReSharper disable once MemberCanBePrivate.Global
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public QlItemsContainer<TDto>? Connection { get; private set; }


    [OnDeserialized]
    // ReSharper disable once UnusedMember.Global
    // ReSharper disable once UnusedParameter.Global
    internal void OnDeserializedMethod(StreamingContext context)
    {
        Connection = (QlItemsContainer<TDto>?)_additionalData.Values.First().ToObject(typeof(QlItemsContainer<TDto>));
    }
}
