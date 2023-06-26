using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Meshmakers.Octo.Common.Shared.DataTransferObjects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;

/// <summary>
/// Represents a RtEntity data transfer object with associations.
/// </summary>
// ReSharper disable once UnusedType.Global
public class QlRtEntityDtoWithAssociations : RtEntityDto
{
    // ReSharper disable once CollectionNeverUpdated.Local
    [JsonExtensionData] private readonly IDictionary<string, JToken> _additionalData;

    /// <summary>
    ///     Constructor
    /// </summary>
    // ReSharper disable once MemberCanBeProtected.Global
    public QlRtEntityDtoWithAssociations()
    {
        _additionalData = new Dictionary<string, JToken>();
    }


    [OnDeserialized]
    // ReSharper disable once UnusedMember.Global
    // ReSharper disable once UnusedParameter.Global
    internal void OnDeserializedMethod(StreamingContext context)
    {
        var x = this.GetType().GetRuntimeProperties().Where(a => a.GetCustomAttribute<QlConnectionAttribute>() != null);

        foreach (var propertyInfo in x)
        {
            var attribute = propertyInfo.GetCustomAttribute<QlConnectionAttribute>();
            if (attribute != null)
            {
                var token = _additionalData[attribute.AssociationName][attribute.ConnectionName];
                if (token != null)
                {
                    var value = token.ToObject(propertyInfo.PropertyType);
                    propertyInfo.SetValue(this, value);
                }
            }
        }
    }
}