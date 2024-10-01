using LiteDB;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Buffering;

/// <summary>
/// Class to convert between JTokens and BsonValue
/// </summary>
public class LiteDbBsonConverter
{
    /// <summary>
    /// Convert a JToken to a Dictionary of string to BsonValue
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public Dictionary<string, BsonValue> JTokenToDictionary(JToken? token)
    {
        if (token == null || token.Type == JTokenType.Null)
        {
            return new Dictionary<string, BsonValue>();
        }

        if (token.Type != JTokenType.Object)
        {
            throw new ArgumentException(
                "The provided JToken is not an object and cannot be converted to a Dictionary.");
        }

        var obj = (JObject)token;
        var dict = new Dictionary<string, BsonValue>();

        foreach (var prop in obj.Properties())
        {
            dict[prop.Name] = JTokenToBsonValue(prop.Value);
        }

        return dict;
    }

    /// <summary>
    /// Convert a JToken to a BsonValue
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public BsonValue JTokenToBsonValue(JToken? token)
    {
        if (token == null)
        {
            return BsonValue.Null;
        }

        switch (token.Type)
        {
            case JTokenType.Object:
            {
                var dict = JTokenToDictionary(token);
                return new BsonDocument(dict);
            }
            case JTokenType.Array:
            {
                var array = (JArray)token;
                var bsonArray = new BsonArray();
                foreach (var item in array)
                {
                    bsonArray.Add(JTokenToBsonValue(item));
                }

                return bsonArray;
            }
            case JTokenType.Integer:
                return new BsonValue((long)token);
            case JTokenType.Float:
                return new BsonValue((double)token);
            case JTokenType.String:
                return new BsonValue((string)token!);
            case JTokenType.Boolean:
                return new BsonValue((bool)token);
            case JTokenType.Null:
                return BsonValue.Null;
            case JTokenType.Date:
                return new BsonValue((DateTime)token);
            case JTokenType.Bytes:
                return new BsonValue((byte[])token!);
            case JTokenType.Guid:
                return new BsonValue((Guid)token);
            case JTokenType.Uri:
                return new BsonValue(token.ToString());
            case JTokenType.TimeSpan:
                return new BsonValue((TimeSpan)token);
            default:
                throw new NotSupportedException($"Unsupported JTokenType: {token.Type}");
        }
    }

    /// <summary>
    /// Convert a BsonValue to a JToken
    /// </summary>
    /// <param name="bsonValue"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public JToken BsonValueToJToken(BsonValue? bsonValue)
    {
        if (bsonValue == null || bsonValue.IsNull)
        {
            return JValue.CreateNull();
        }

        switch (bsonValue.Type)
        {
            case BsonType.Document:
            {
                var obj = new JObject();
                foreach (var kvp in bsonValue.AsDocument)
                {
                    obj[kvp.Key] = BsonValueToJToken(kvp.Value);
                }
                return obj;
            }
            case BsonType.Array:
            {
                var array = new JArray();
                foreach (var item in bsonValue.AsArray)
                {
                    array.Add(BsonValueToJToken(item));
                }
                return array;
            }
            case BsonType.Int32:
                return new JValue(bsonValue.AsInt32);
            case BsonType.Int64:
                return new JValue(bsonValue.AsInt64);
            case BsonType.Double:
                return new JValue(bsonValue.AsDouble);
            case BsonType.Decimal:
                return new JValue(bsonValue.AsDecimal);
            case BsonType.String:
                return new JValue(bsonValue.AsString);
            case BsonType.Boolean:
                return new JValue(bsonValue.AsBoolean);
            case BsonType.DateTime:
                return new JValue(bsonValue.AsDateTime);
            case BsonType.Guid:
                return new JValue(bsonValue.AsGuid.ToString());
            case BsonType.Binary:
                return new JValue(Convert.ToBase64String(bsonValue.AsBinary));
            case BsonType.Null:
                return JValue.CreateNull();
            default:
                throw new NotSupportedException($"Unsupported BsonType: {bsonValue.Type}");
        }
    }

    /// <summary>
    /// Converts a Dictionary of string to BsonValue to a JToken
    /// </summary>
    /// <param name="dict"></param>
    /// <returns></returns>
    public JToken DictionaryToJToken(Dictionary<string, BsonValue> dict)
    {
        var obj = new JObject();
        foreach (var kvp in dict)
        {
            obj[kvp.Key] = BsonValueToJToken(kvp.Value);
        }
        return obj;
    }
    
    /// <summary>
    /// Merges multiple dictionaries into one
    /// </summary>
    /// <param name="dictionaries"></param>
    /// <returns></returns>
    public Dictionary<string, BsonValue> MergeDictionaries(params Dictionary<string, BsonValue>[] dictionaries)
    {
        var result = new Dictionary<string, BsonValue>();

        foreach (var dict in dictionaries)
        {
            foreach (var kvp in dict)
            {
                string key = kvp.Key;
                BsonValue value = kvp.Value;

                if (!result.ContainsKey(key))
                {
                    // Key not in result, add it
                    result[key] = value;
                }
                else
                {
                    // Key exists, merge the values
                    var existingValue = result[key];
                    result[key] = MergeBsonValues(existingValue, value);
                }
            }
        }

        return result;
    }

    private BsonValue MergeBsonValues(BsonValue existingValue, BsonValue newValue)
    {
        if (existingValue.IsArray && newValue.IsArray)
        {
            // Both are arrays, merge them
            var mergedArray = existingValue.AsArray;
            mergedArray.AddRange(newValue.AsArray);
            return mergedArray;
        }
        else if (existingValue.IsDocument && newValue.IsDocument)
        {
            // Both are documents, merge them recursively
            var mergedDoc = MergeBsonDocuments(existingValue.AsDocument, newValue.AsDocument);
            return mergedDoc;
        }
        else if (existingValue.IsArray)
        {
            // Existing is array, add new value to it
            var array = existingValue.AsArray;
            array.Add(newValue);
            return array;
        }
        else if (newValue.IsArray)
        {
            // New value is array, add existing value to it
            var array = new BsonArray { existingValue };
            array.AddRange(newValue.AsArray);
            return array;
        }
        else
        {
            // Neither is array or document, combine into an array
            var array = new BsonArray { existingValue, newValue };
            return array;
        }
    }

    private BsonDocument MergeBsonDocuments(BsonDocument existingDoc, BsonDocument newDoc)
    {
        foreach (var kvp in newDoc)
        {
            if (existingDoc.TryGetValue(kvp.Key, out var existingValue))
            {
                existingDoc[kvp.Key] = MergeBsonValues(existingValue, kvp.Value);
            }
            else
            {
                existingDoc.Add(kvp.Key, kvp.Value);
            }
        }
        return existingDoc;
    }
}